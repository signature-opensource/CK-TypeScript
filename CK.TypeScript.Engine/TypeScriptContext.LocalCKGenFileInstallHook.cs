using CK.BinarySerialization;
using CK.Core;
using CK.Transform.Core;
using CK.TypeScript.CodeGen;
using System.Collections.Generic;
using System.IO;

namespace CK.Setup;


public sealed partial class TypeScriptContext // '@local/ck-gen' processor.
{
    sealed class LocalCKGenFileInstallHook : TransformableFileInstallHook
    {
        readonly TSTypeManager _tsTypes;
        TransformerHost.Language? _tsLanguage;

        public LocalCKGenFileInstallHook( TSTypeManager tsTypes )
        {
            _tsTypes = tsTypes;
        }

        public override void StartInstall( IActivityMonitor monitor )
        {
            Throw.DebugAssert( IsInitialized );
            _tsLanguage = TransformerHost.FindLanguage( "TypeScript", withFileExtensions: false );
            if( _tsLanguage == null )
            {
                monitor.Warn( "Missing TypeScript language registration in TransformerHost." );
            }
        }

        public override bool HandleInstall( IActivityMonitor monitor, ITransformInstallableItem item, ref string finalText, IResourceSpaceItemInstaller installer, out bool handled )
        {
            Throw.DebugAssert( IsInitialized );
            handled = false;
            if( _tsLanguage != null
                && item.LanguageIndex == _tsLanguage.Index
                && finalText.Contains( "'@local/ck-gen'" ) )
            {

            }
            return true;
        }

        public override void StopInstall( IActivityMonitor monitor, bool success, IResourceSpaceItemInstaller installer )
        {
            // Nothing to do.
        }

        protected override bool ShouldWriteLiveState => _tsLanguage != null;

        protected override void WriteLiveState( IBinarySerializer s )
        {
            Throw.DebugAssert( IsInitialized && _tsLanguage != null );
            s.Writer.Write( _tsLanguage.Index );
            foreach( var t in _tsTypes.DeclaredTypes )
            {
                s.Writer.WriteSharedString( t.File.Folder.Path );
                s.Writer.Write( Path.GetFileNameWithoutExtension( t.File.Name ) );
                s.Writer.Write( t.TypeName );
            }
            s.Writer.WriteSharedString( null );
        }

        public static ILiveTransformableFileInstallHook? ReadLiveState( IActivityMonitor monitor,
                                                                        ResSpaceData spaceData,
                                                                        IBinaryDeserializer d )
        {
            int tsLanguageIndex = d.Reader.ReadInt32();
            Dictionary<string, string> typeMapping = new Dictionary<string, string>();
            for( ; ; )
            {
                var fPath = d.Reader.ReadSharedString();
                if( fPath == null ) break;
                fPath += d.Reader.ReadString();
                var typeName = d.Reader.ReadString();
                typeMapping.Add( typeName, fPath );
            }
            return new LiveHook( tsLanguageIndex, typeMapping );
        }

        sealed class LiveHook : ILiveTransformableFileInstallHook
        {
            readonly int _tsLanguageIndex;
            readonly Dictionary<string, string> _typeMapping;

            public LiveHook( int tsLanguageIndex, Dictionary<string, string> typeMapping )
            {
                _tsLanguageIndex = tsLanguageIndex;
                _typeMapping = typeMapping;
            }

            public void StartInstall( IActivityMonitor monitor )
            {
            }

            public bool HandleRemove( IActivityMonitor monitor,
                                      ITransformInstallableItem item,
                                      ILiveResourceSpaceItemInstaller installer,
                                      out bool handled )
            {
                // Nothing to do.
                handled = false;
                return true;
            }

            public bool HandleInstall( IActivityMonitor monitor,
                                       ITransformInstallableItem item,
                                       ref string finalText,
                                       IResourceSpaceItemInstaller installer,
                                       out bool handled )
            {
                handled = false;
                if( item.LanguageIndex == _tsLanguageIndex
                    && finalText.Contains( "'@local/ck-gen'" ))
                {
                }
                return true;
            }

            public void StopInstall( IActivityMonitor monitor, bool success, IResourceSpaceItemInstaller installer )
            {
                // Nothing to do.
            }
        }

    }
}
