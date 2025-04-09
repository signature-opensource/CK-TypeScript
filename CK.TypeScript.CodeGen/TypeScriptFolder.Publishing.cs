using CK.Core;
using CommunityToolkit.HighPerformance;
using System;
using System.Linq;
using System.Text;

namespace CK.TypeScript.CodeGen;

public sealed partial class TypeScriptFolder
{
    internal ref struct PublishContext
    {
        readonly Span<char> _pathBuffer;
        readonly ITypeScriptPublishTarget _target;
        ReadOnlySpan<char> _path;
        public readonly StringBuilder BarrelStringBuilder;

        internal PublishContext( Span<char> pathBuffer, ITypeScriptPublishTarget target )
        {
            _pathBuffer = pathBuffer;
            _target = target;
            BarrelStringBuilder = new StringBuilder();
        }

        public void EnterFolder( string name )
        {
            name.AsSpan().CopyTo( _pathBuffer.Slice( _path.Length ) );
            int len = _path.Length + name.Length;
            _path = _pathBuffer.Slice( 0, len + 1 );
            _pathBuffer[len] = '/';
        }

        public void LeaveFolder( string name )
        {
            Throw.DebugAssert( _path.Length > 0 && _path.EndsWith( name + '/' ) );
            _path = _pathBuffer.Slice( 0, _path.Length - name.Length - 1 );
        }

        public void Publish( string name, string content )
        {
            name.AsSpan().CopyTo( _pathBuffer.Slice( _path.Length ) );
            _target.Add( _pathBuffer.Slice( 0, _path.Length + name.Length ), content );
        }
    }

    internal void Publish( IActivityMonitor monitor, ref PublishContext target )
    {
        // Skips empty folder (recursively thanks to the lifted _fileCount).
        if( _fileCount == 0 ) return;
        Throw.DebugAssert( _firstChild != null || _firstFile != null );
        var cFolder = _firstChild;
        var cFile = _firstFile;
        using( monitor.OpenTrace( IsRoot ? "Publishing TypeScript Root folder." : $"Saving /{Name}." ) )
        {
            if( !IsRoot ) target.EnterFolder( Name );
            bool hasBarrel = false;
            do
            {
                // Publish files until a folder has a lexically ordered greater name including the separator!
                while( cFile != null && (cFolder == null || cFile.Name.AsSpan().CompareTo( cFolder.NameWithSeparator, StringComparison.Ordinal ) < 0) )
                {
                    if( !hasBarrel && cFile.Name.Equals( "index.ts", StringComparison.OrdinalIgnoreCase ) )
                    {
                        hasBarrel = true;
                        monitor.Trace( "Publishing existing 'index.ts' barrel file." );
                    }
                    else
                    {
                        monitor.Trace( $"Publishing '{cFile.Name}'." );
                    }
                    target.Publish( cFile.Name, cFile.GetCurrentText() );
                    cFile = cFile._next;
                }
                // Publish the folder and continue on files.
                if( cFolder != null )
                {
                    cFolder.Publish( monitor, ref target );
                    cFolder = cFolder._next;
                }
            }
            while( cFile != null || cFolder != null );
            if( !IsRoot ) target.LeaveFolder( Name );

            if( _wantBarrel && !hasBarrel && _hasExportedSymbol )
            {
                var b = target.BarrelStringBuilder;
                Throw.DebugAssert( b.Length == 0 );
                AddExportsToBarrel( "/", b );
                Throw.DebugAssert( "Because HasExportedSymbol.", b.Length > 0 );
                monitor.Trace( "Publishing automatically generated 'index.ts' barrel." );
                target.Publish( "index.ts", b.ToString() );
                b.Clear();
            }
        }
    }

    void AddExportsToBarrel( string subPath, StringBuilder b )
    {
        if( subPath.Length > 1 && _wantBarrel && _hasExportedSymbol )
        {
            b.Append( "export * from '." ).Append( subPath ).AppendLine( "';" );
        }
        else
        {
            var file = _firstFile;
            while( file != null )
            {
                if( file is TypeScriptFileBase ts && ts.AllTypes.Any() )
                {
                    AddExportFile( subPath, b, file.Name.AsSpan().Slice( 0, file.Name.Length - 3 ) );
                }
                file = file._next;
            }
            var folder = _firstChild;
            while( folder != null )
            {
                folder.AddExportsToBarrel( subPath + folder.NameWithSeparator.ToString(), b );
                folder = folder._next;
            }

            static void AddExportFile( string subPath, StringBuilder b, ReadOnlySpan<char> fileName )
            {
                b.Append( "export * from '." ).Append( subPath );
                //if( subPath.Length > 0 ) b.Append( '/' );
                b.Append( fileName ).AppendLine( "';" );
            }
        }
    }
}
