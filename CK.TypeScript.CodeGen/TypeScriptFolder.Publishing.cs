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
        int _folderDepth;
        readonly StringBuilder? _summary;
        readonly StringBuilder? _exportedTypesSummary;
        public readonly StringBuilder BarrelStringBuilder;
        public readonly TSTypeManager TSTypes;

        internal PublishContext( Span<char> pathBuffer,
                                 ITypeScriptPublishTarget target,
                                 TSTypeManager tsTypes,
                                 bool withSummary )
        {
            _pathBuffer = pathBuffer;
            _target = target;
            TSTypes = tsTypes;
            if( withSummary )
            {
                _summary = new StringBuilder( "Summary:" );
                _summary.AppendLine();
                _exportedTypesSummary = new StringBuilder();
            }
            else
            {
                _summary = null;
            }
            BarrelStringBuilder = new StringBuilder();
        }

        internal void EnterFolder( string name )
        {
            name.AsSpan().CopyTo( _pathBuffer.Slice( _path.Length ) );
            int len = _path.Length + name.Length;
            _path = _pathBuffer.Slice( 0, len + 1 );
            _pathBuffer[len] = '/';
            if( HasSummary ) AppendLinePrefix().Append( '/' ).Append( name ).AppendLine();
            ++_folderDepth;
        }

        internal void LeaveFolder( string name )
        {
            Throw.DebugAssert( _path.Length > 0 && _path.EndsWith( name + '/' ) );
            _path = _pathBuffer.Slice( 0, _path.Length - name.Length - 1 );
            --_folderDepth;
        }

        internal readonly void Publish( string name, string content )
        {
            name.AsSpan().CopyTo( _pathBuffer.Slice( _path.Length ) );
            _target.Add( _pathBuffer.Slice( 0, _path.Length + name.Length ), content );
        }

        internal readonly bool HasSummary => _summary != null;

        internal readonly void SummaryOnAppendIndex()
        {
            AppendLinePrefix().Append( "-> 'index.ts' (manual)." ).AppendLine();
        }

        internal readonly void SummaryOnPublishedResource( ResourceTypeScriptFile rFile )
        {
            AppendLinePrefix().Append( "-> [Res] '" ).Append( rFile.Name ).Append( "' is moved to <Code> container from " ).Append( rFile.Locator );
            DumpCKomposableExportedTypes( rFile );
        }

        internal readonly void SummaryOnUnpublishedResource( ResourceTypeScriptFile rFile )
        {
            AppendLinePrefix().Append( "-> [Res] '" ).Append( rFile.Name ).Append( '\'' );
            DumpCKomposableExportedTypes( rFile );
        }

        internal readonly void SummaryOnTypeScriptFile( TypeScriptFileBase cFile )
        {
            AppendLinePrefix().Append( "-> [Code] '" ).Append( cFile.Name );
            DumpCKomposableExportedTypes( cFile );
        }

        readonly StringBuilder AppendLinePrefix()
        {
            Throw.DebugAssert( _summary != null );
            return _summary.Append( ' ', 3 * _folderDepth );
        }

        readonly void DumpCKomposableExportedTypes( TypeScriptFileBase f )
        {
            Throw.DebugAssert( _summary != null && _exportedTypesSummary != null );
            var e = f.AllTypes.GetEnumerator();
            if( e.MoveNext() )
            {
                _exportedTypesSummary.Append( "import { " );

                _summary.AppendLine();
                AppendLinePrefix().Append( "   Exported types: " );
                bool atLeastOne = false;
                do
                {
                    if( atLeastOne )
                    {
                        _summary.Append( ", " );
                        _exportedTypesSummary.Append( ", " );
                    }

                    atLeastOne = true;
                    _summary.Append( '\'' ).Append( e.Current.TypeName ).Append( '\'' );

                    _exportedTypesSummary.Append( e.Current.TypeName );
                }
                while( e.MoveNext() );

                _exportedTypesSummary.Append( " } from '@local/ck-gen/" )
                        .Append( f.Folder.Path ).Append( f.Name )
                        .Append( "';" )
                        .AppendLine();
            }
            else _summary.Append( " - No exported types" );
            _summary.Append( '.' ).AppendLine();
        }

        internal readonly void EmitSummary( IActivityMonitor monitor )
        {
            Throw.DebugAssert(_summary != null && _exportedTypesSummary != null );
            monitor.Info( _summary.ToString() );
            using( monitor.OpenInfo( "Auto resolvable imports from '@local/ck-gen' are:" ) )
            {
                monitor.Info( _exportedTypesSummary.ToString() );
            }
        }
    }

    internal void Publish( IActivityMonitor monitor, ref PublishContext target )
    {
        // Skips empty folder (recursively thanks to the lifted _fileCount).
        if( _fileCount == 0 ) return;
        Throw.DebugAssert( _firstChild != null || _firstFile != null );
        var cFolder = _firstChild;
        var cFile = _firstFile;
        if( IsRoot )
        {
            monitor.OpenInfo( "Published TypeScript Root folder to the <Code> generated container." );
        }
        else
        {
            target.EnterFolder( Name );
        }
        bool hasBarrel = false;
        do
        {
            // Publish files until a folder has a lexically ordered greater name including the separator!
            while( cFile != null
                    && (cFolder == null || cFile.Name.AsSpan().CompareTo( cFolder.NameWithSeparator, StringComparison.Ordinal ) < 0) )
            {
                if( !hasBarrel && cFile.Name.Equals( "index.ts", StringComparison.OrdinalIgnoreCase ) )
                {
                    hasBarrel = true;
                    monitor.Warn( $"Publishing manual '{cFile.Folder.Path}index.ts' barrel file, it MUST export all relevant types from this folder and below." );
                    if( target.HasSummary ) target.SummaryOnAppendIndex();
                    target.Publish( cFile.Name, cFile.GetCurrentText( monitor, target.TSTypes ) );
                }
                else if( cFile is ResourceTypeScriptFile rFile )
                {
                    if( rFile.IsPublishedResource )
                    {
                        if( target.HasSummary ) target.SummaryOnPublishedResource( rFile );
                        target.Publish( rFile.Name, rFile.GetCurrentText( monitor, target.TSTypes ) );
                    }
                    else
                    {
                        if( target.HasSummary ) target.SummaryOnUnpublishedResource( rFile );
                    }
                }
                else
                {
                    Throw.DebugAssert( cFile is TypeScriptFile );
                    if( target.HasSummary ) target.SummaryOnTypeScriptFile( cFile );
                    target.Publish( cFile.Name, cFile.GetCurrentText( monitor, target.TSTypes ) );
                }
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

        if( IsRoot )
        {
            if( target.HasSummary )
            {
                target.EmitSummary( monitor );
            }
            monitor.CloseGroup();
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
