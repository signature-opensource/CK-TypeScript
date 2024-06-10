using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    sealed class FileImportCodePart : ITSFileImportSection
    {
        readonly TypeScriptFile _file;
        List<(TypeScriptFile File, List<string> Types)>? _importFiles;
        List<(string LibraryName, List<string> Types)>? _importLibs;
        int _importCount;

        public FileImportCodePart( TypeScriptFile f )
        {
            _file = f;
        }

        public ITSFileImportSection EnsureImportFromLibrary( LibraryImport libraryImport, string typeName, params string[] typeNames )
        {
            _file.Root.LibraryManager.EnsureLibrary( libraryImport );
            Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
            AddTypeNames( ref _importLibs, libraryImport.Name, typeName, typeNames );
            return this;
        }

        public ITSFileImportSection EnsureImport( TypeScriptFile file, string typeName, params string[] typeNames )
        {
            Throw.CheckNotNullArgument( file );
            Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
            if( file != _file )
            {
                AddTypeNames( ref _importFiles, file, typeName, typeNames );
            }
            return this;
        }

        void AddTypeNames<TKey>( [AllowNull] ref List<(TKey, List<string>)> imports, TKey key, string? typeName, IEnumerable<string> typeNames )
            where TKey : class
        {
            imports ??= new List<(TKey, List<string>)>();
            List<string>? types = null;
            foreach( var e in imports )
            {
                if( e.Item1 == key )
                {
                    types = e.Item2;
                    break;
                }
            }
            if( types == null )
            {
                types = new List<string>();
                imports.Add( (key, types) );
            }
            if( typeName != null )
            {
                Add( types, typeName, ref _importCount );
            }
            foreach( var t in typeNames )
            {
                Add( types, t, ref _importCount );
            }

            static void Add( List<string> types, string typeName, ref int importCount )
            {
                if( !types.Contains( typeName ) )
                {
                    ++importCount;
                    types.Add( typeName );
                }
            }
        }

        public ITSFileImportSection EnsureImport( ITSType tsType, params ITSType[] tsTypes )
        {
            Throw.CheckArgument( tsType != null && tsTypes != null );
            tsType.EnsureRequiredImports( this );
            foreach( var t in tsTypes )
            {
                t.EnsureRequiredImports( this );
            }
            return this;
        }

        public ITSFileImportSection EnsureImport( IActivityMonitor monitor, Type type, params Type[] types )
        {
            Throw.CheckArgument( monitor != null && types != null );
            ImportType( monitor, type );
            foreach( var t in types )
            {
                ImportType( monitor, t );
            }
            return this;
        }

        public ITSFileImportSection EnsureImport( IActivityMonitor monitor, IEnumerable<Type> types )
        {
            Throw.CheckArgument( monitor != null && types != null );
            foreach( var t in types )
            {
                ImportType( monitor, t );
            }
            return this;
        }

        void ImportType( IActivityMonitor monitor, Type type )
        {
            Throw.CheckNotNullArgument( type );
            var tsType = _file.Root.TSTypes.ResolveTSType( monitor, type );
            tsType.EnsureRequiredImports( this );
        }

        public int ImportCount => _importCount;

        public IEnumerable<string> ImportedLibraryNames => _importLibs?.Select( l => l.LibraryName ) ?? ImmutableArray<string>.Empty;

        internal Action<ITSFileImportSection> CreateImportSnapshotAndClear()
        {
            if( _importCount == 0 )
            {
                Throw.DebugAssert( (_importFiles?.Count ?? 0) == 0 && (_importLibs?.Count ?? 0) == 0 );
                return _ => { };
            }
            // Snapshot is array of arrays:
            // - First are object[]: [TypeScriptFile, type names...]
            // - Then a string[]: [library name, type names...] 
            IEnumerable<object> all = _importFiles != null
                                        ? _importFiles.Select( l => l.Types.Cast<object>().Prepend( l.File ).ToArray() )
                                        : Enumerable.Empty<object>();
            if( _importLibs != null )
            {
                all = all.Append( _importLibs.Select( l => l.Types.Prepend( l.LibraryName ) ).ToArray() );
            }

            var snapshot = all.ToArray();

            _importFiles?.Clear();
            _importLibs?.Clear();
            _importCount = 0;

            return i => ((FileImportCodePart)i).ImportSnapshot( snapshot );
        }

        void ImportSnapshot( object snapshot )
        {
            var all = (object[])snapshot;
            foreach( var o in all )
            {
                if( o is string[] libs )
                {
                    AddTypeNames( ref _importLibs, libs[0], null, libs.Skip( 1 ) );
                }
                else
                {
                    var files = (object[])o;
                    AddTypeNames( ref _importFiles, (TypeScriptFile)files[0], null, files.Skip( 1 ).Cast<string>() );
                }
            }
        }

        internal SmarterStringBuilder Build( SmarterStringBuilder b )
        {
            if( _importCount > 0  )
            {
                var import = new BaseCodeWriter( _file );
                if( _importLibs != null )
                {
                    foreach( var (libraryName, types) in _importLibs )
                    {
                        import.Append( "import { " ).Append( types ).Append( " } from " )
                              .AppendSourceString( libraryName ).Append( ";" ).NewLine();
                    }
                }
                if( _importFiles != null )
                {
                    foreach( var (file, types) in _importFiles )
                    {
                        import.Append( "import { " ).Append( types ).Append( " } from " )
                              .AppendSourceString( _file.Folder.GetRelativePathTo( file.Folder ).AppendPart( file.Name.Remove( file.Name.Length - 3 ) ) )
                              .Append( ";" ).NewLine();
                    }
                }
                import.Build( b );
            }
            return b;
        }

        public override string ToString()
        {
            var b = new StringBuilder();
            Build( new SmarterStringBuilder( b ) );
            if( b.Length > 0 ) b.Append( Environment.NewLine );
            return b.ToString();
        }
    }
}
