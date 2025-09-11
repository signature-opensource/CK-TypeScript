using CK.BinarySerialization;
using CK.Core;
using CK.Transform.Core;
using CK.TypeScript.CodeGen;
using CK.TypeScript.Transform;
using System;
using System.Collections.Generic;
using System.Linq;

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
                return ProcessFinalText( monitor,
                                         item,
                                         ref finalText,
                                         _tsLanguage.TargetLanguageAnalyzer,
                                         typeName => (_tsTypes.FindByTypeName( typeName ) as ITSDeclaredFileType)?.ImportPath );
            }
            return true;
        }

        static bool ProcessFinalText( IActivityMonitor monitor,
                                      ITransformInstallableItem item,
                                      ref string finalText,
                                      ITargetAnalyzer tsAnalyzer,
                                      Func<string, string?> declaredFilePath )
        {
            // One alternative here:
            // - We can use a parsing and EnsureImport API.
            // - Or we use a brutal regex approach, considering that
            //   only named imports can be done from '@local/ck-gen' barrel
            //   (no default, no namaspace, no side-effect only) but there may be
            //   type only imports, and aliased types...
            // => We chose to use the EnsureImport API by extending it to support "more precise"
            //    import paths. This may be less performant than the regex approach but this
            //    is more solid.
            var code = tsAnalyzer.TryParse( monitor, finalText )?.SourceCode;
            if( code == null )
            {
                monitor.Error( $"Failed to parse {item.Origin} text." );
                return false;
            }
            bool success = true;
            // Consider all the named imports from '@local/ck-gen'.
            // If the import is a "ipmort type {...}", we project the TypeOnly on each imported name.
            var allNamedImports = code.Spans.OfType<ImportStatement>()
                                           .Where( i => i.ImportPath == "@local/ck-gen" )
                                           .SelectMany( i => i.TypeOnly
                                                                ? i.NamedImports.Select( n => n with { TypeOnly = true } )
                                                                : i.NamedImports )
                                           .ToList();
            if( allNamedImports.Count > 0 )
            {
                using( monitor.OpenTrace( $"Transforming import from '@local/ck-gen' in {item.Origin} to their declaring file for {allNamedImports.Count} types." ) )
                using( var editor = new SourceCodeEditor( monitor, code, tsAnalyzer ) )
                {
                    foreach( var import in allNamedImports )
                    {
                        // We use the ExportedName here to find the type's file, not the potential alias name used.
                        var path = declaredFilePath( import.ExportedName );
                        if( path == null )
                        {
                            monitor.Error( $"Failed to find a file for type '{import.ExportedName}' in import from '@local/ck-gen'." );
                            success = false;
                        }
                        else
                        {
                            var importLine = new ImportLine { NamedImports = { import }, ImportPath = "@local/ck-gen/" + path };
                            EnsureImportStatement.EnsureImport( monitor, editor, importLine );
                            editor.SetNeedReparse();
                        }
                    }
                    if( success && editor.NeedReparse )
                    {
                        if( !editor.Reparse() )
                        {
                            return false;
                        }
                        finalText = code.ToString();
                    }
                }
            }
            return success;
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
                s.Writer.WriteNullableString( t.ImportPath );
                s.Writer.Write( t.TypeName );
            }
            s.Writer.WriteNullableString( null );
        }

        public static ILiveTransformableFileInstallHook? ReadLiveState( IActivityMonitor monitor,
                                                                        ResCoreData spaceData,
                                                                        IBinaryDeserializer d )
        {
            int tsLanguageIndex = d.Reader.ReadInt32();
            Dictionary<string, string> typeMapping = new Dictionary<string, string>();
            for( ; ; )
            {
                var fPath = d.Reader.ReadNullableString();
                if( fPath == null ) break;
                typeMapping.Add( d.Reader.ReadString(), fPath );
            }
            return new LiveHook( tsLanguageIndex, typeMapping );
        }

        sealed class LiveHook : ILiveTransformableFileInstallHook
        {
            readonly int _tsLanguageIndex;
            readonly TypeScriptAnalyzer _tsAnalyser;
            readonly Dictionary<string, string> _typeMapping;

            public LiveHook( int tsLanguageIndex, Dictionary<string, string> typeMapping )
            {
                _tsLanguageIndex = tsLanguageIndex;
                _tsAnalyser = new TypeScriptAnalyzer();
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
                    return ProcessFinalText( monitor,
                                             item,
                                             ref finalText,
                                             _tsAnalyser,
                                             _typeMapping.GetValueOrDefault );
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
