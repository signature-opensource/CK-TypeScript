using CK.BinarySerialization;
using CK.Transform.Core;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Core;

public sealed partial class TransformableFileHandler : ILiveResourceSpaceHandler
{
    static Type[] _installHookReadParameters = new[] { typeof( IActivityMonitor ), typeof( IBinaryDeserializer ) };

    /// <summary>
    /// Live update is currently supported only when installing on the file system:
    /// the <see cref="FileSystemInstaller.TargetPath"/> is serialized in the live state
    /// and a basic <see cref="FileSystemInstaller"/> is deserialized to be the installer
    /// on the live side.
    /// </summary>
    public bool DisableLiveUpdate => Installer is not FileSystemInstaller;

    /// <summary>
    /// Writes the <see cref="FileSystemInstaller.TargetPath"/> and the <see cref="TransformLanguage"/> types
    /// of the <see cref="TransformerHost"/> and the <see cref="TransformEnvironment"/>.
    /// The TransformLanguage must have a public default constructor otherwise an error is logged
    /// and false is returned.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="s">The serializer for the primary <see cref="ResSpace.LiveStateFileName"/>.</param>
    /// <param name="spaceData">The resource space that has been serialized.</param>
    /// <returns>True on success, false on error. Errorrs must be logged.</returns>
    public bool WriteLiveState( IActivityMonitor monitor, IBinarySerializer s, ResSpaceData spaceData )
    {
        Throw.DebugAssert( "Otherwise LiveState would have been disabled.", Installer is FileSystemInstaller );
        Throw.DebugAssert( "Environment is null only during deserialization.", _environment != null );

        s.Writer.Write( ((FileSystemInstaller)Installer).TargetPath );
        foreach( var hook in _installHooks )
        {
            Throw.DebugAssert( hook is TransformableFileInstallHook );
            var h = Unsafe.As<TransformableFileInstallHook>( hook );
            if( h.ShouldWriteLiveState )
            {
                s.Writer.Write( true );
                s.WriteTypeInfo( h.GetType() );
                h.WriteLiveState( s );
            }
        }
        s.Writer.Write( false );
        // Writes the types of the TransformLanguage.
        Throw.DebugAssert( "There is no AutoLanguage in languages.", !_transformerHost.Languages.Any( l => l.IsAutoLanguage ) );
        s.Writer.WriteNonNegativeSmallInt32( _transformerHost.Languages.Count );
        foreach( var l in _transformerHost.Languages.Select( l => l.TransformLanguage ) )
        {
            var t = l.GetType();
            if( t.GetConstructor( Type.EmptyTypes ) == null )
            {
                monitor.Error( $"""
                    TransformLanguage '{t:C}' cannot be serialized because it misses a default public constructor.
                    Unable to serialize the TransformerHost.
                    """ );
                return false;
                    
            }
            s.WriteTypeInfo( l.GetType() );
        }
        _environment.Serialize( s );
        return true;
    }

    /// <summary>
    /// Restores a <see cref="ILiveUpdater"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="spaceData">The deserialized resource space data.</param>
    /// <param name="d">The deserializer for the primary <see cref="ResSpace.LiveStateFileName"/>.</param>
    /// <returns>The live updater on success, null on error. Errors are logged.</returns>
    public static ILiveUpdater? ReadLiveState( IActivityMonitor monitor, ResSpaceData spaceData, IBinaryDeserializer d )
    {
        var installer = new FileSystemInstaller( d.Reader.ReadString() );

        var hooksBuilder = ImmutableArray.CreateBuilder<ITransformableFileInstallHook>();
        bool success = ReadInstallHooks( monitor, d, hooksBuilder );
        if( !success ) return null;
        var installHooks = hooksBuilder.DrainToImmutable();

        var languages = new TransformLanguage[d.Reader.ReadNonNegativeSmallInt32()];
        for( int i = 0; i < languages.Length; ++i )
        {
            var t = d.ReadTypeInfo().ResolveLocalType();
            languages[i] = (TransformLanguage)Activator.CreateInstance( t )!;
        }
        var transformerHost = new TransformerHost( languages );
        var environment = new TransformEnvironment( spaceData, transformerHost, d );
        environment.PostDeserialization( monitor );
        return new LiveState( environment, installHooks, installer );
    }

    private static bool ReadInstallHooks( IActivityMonitor monitor, IBinaryDeserializer d, ImmutableArray<ITransformableFileInstallHook>.Builder hooksBuilder )
    {
        bool success = true;
        object[] installHookReadArgs = [monitor, d];
        while( d.Reader.ReadBoolean() )
        {
            var tHook = d.ReadTypeInfo().ResolveLocalType();
            var mRead = tHook.GetMethod( "ReadLiveState", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, _installHookReadParameters );
            if( mRead == null || mRead.ReturnType != typeof( ITransformableFileInstallHook ) )
            {
                monitor.Error( $"""
                    Type '{tHook:C}' requires a method:
                    public static ITransformableFileInstallHook? ReadLiveState( IActivityMonitor monitor, IBinaryDeserializer d );
                    """ );
                success = false;
            }
            else
            {
                var o = mRead.Invoke( null, installHookReadArgs ) as ITransformableFileInstallHook;
                if( o != null )
                {
                    hooksBuilder.Add( o );
                }
                else
                {
                    monitor.Error( $"Type '{tHook:C}' failed to load its Live state." );
                    success = false;
                }
            }
        }

        return success;
    }

    sealed class LiveState : ILiveUpdater
    {
        readonly TransformEnvironment _environment;
        readonly ImmutableArray<ITransformableFileInstallHook> _installHooks;
        readonly FileSystemInstaller _installer;
        int _changeCount;

        public LiveState( TransformEnvironment environment, ImmutableArray<ITransformableFileInstallHook> hooks, FileSystemInstaller installer )
        {
            _environment = environment;
            _installHooks = hooks;
            _installer = installer;
        }

        public void ApplyChanges( IActivityMonitor monitor )
        {
            var c = _changeCount;
            _changeCount = 0;
            if( c != 0 )
            {
                var toBeInstalled = _environment.Tracker.ApplyChanges( monitor, _environment );
                if( toBeInstalled != null )
                {
                    var installer = new InstallHooksHelper( _installHooks, _installer );
                    installer.Start( monitor );
                    foreach( var i in toBeInstalled )
                    {
                        var text = i.GetFinalText( monitor, _environment.TransformerHost );
                        if( text != null )
                        {
                            installer.Handle( monitor, new TransformInstallableItem( i, _environment.TransformerHost ), text );
                        }
                    }
                    installer.Stop( monitor );
                }
            }
        }

        public bool OnChange( IActivityMonitor monitor, PathChangedEvent changed )
        {
            Throw.DebugAssert( changed.Resources.LocalPath != null );
            // This is NOT right!
            if( _environment.TransformerHost.FindFromFilename( changed.SubPath, out _ ) != null )
            {
                if( _environment.Tracker.OnChange( monitor, changed ) )
                {
                    ++_changeCount;
                }
                return true;
            }
            return false;
        }
    }
}
