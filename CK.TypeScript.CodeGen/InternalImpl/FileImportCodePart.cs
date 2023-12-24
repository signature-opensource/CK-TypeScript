using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CK.TypeScript.CodeGen
{

    class FileImportCodePart : BaseCodeWriter, ITSFileImportSection
    {
        List<(TypeScriptFile File, List<string> Types)>? _importFiles;
        List<(string LibraryName, List<string> Types)>? _importLibs;
        readonly Dictionary<string, LibraryImport> _libraries = new();
        int _importCount;

        public FileImportCodePart( TypeScriptFile f )
            : base( f )
        {
        }

        public IReadOnlyDictionary<string, LibraryImport> LibraryImports => _libraries;

        public ITSFileImportSection EnsureLibrary(LibraryImport libraryImport)
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( libraryImport.Name );
            if( !_libraries.TryGetValue( libraryImport.Name, out var lib ) )
            {
                _libraries[libraryImport.Name] = libraryImport;
            }
            else
            {
                if( lib.Version != libraryImport.Version )
                {
                    Throw.InvalidOperationException( $"Previously imported this library at version {lib.Version}, but currently importing it with version {libraryImport.Version}" );
                }
                if( lib.DependencyKind < libraryImport.DependencyKind )
                {
                    _libraries[libraryImport.Name] = libraryImport;
                }
            }
            foreach( var d in libraryImport.ImpliedDependencies )
            {
                EnsureLibrary( d );
            }
            return this;
        }

        public ITSFileImportSection EnsureImportFromLibrary( LibraryImport libraryImport, string typeName, params string[] typeNames )
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
            EnsureLibrary( libraryImport );
            AddTypeNames( ref _importLibs, libraryImport.Name, typeName, typeNames );
            return this;
        }

        public ITSFileImportSection EnsureImport( TypeScriptFile file, string typeName, params string[] typeNames )
        {
            Throw.CheckNotNullArgument( file );
            if( file != File )
            {
                AddTypeNames( ref _importFiles, file, typeName, typeNames );
            }
            return this;
        }

        List<string> AddTypeNames<TKey>( [AllowNull] ref List<(TKey, List<string>)> imports, TKey key, string typeName, params string[] typeNames ) where TKey : class
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
            if( imports == null ) imports = new List<(TKey, List<string>)>();
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
            Add( types, typeName, ref _importCount );
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

            return types;
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
            var tsType = File.Root.TSTypes.ResolveTSType( monitor, type );
            if( tsType.File != null ) EnsureImport( tsType.File, tsType.TypeName );
        }

        public int ImportCount => _importCount;

        internal override SmarterStringBuilder Build( SmarterStringBuilder b )
        {
            if( _importFiles != null || _importLibs != null )
            {
                var import = new BaseCodeWriter( File );
                if( _importLibs != null )
                {
                    foreach( var e in _importLibs )
                    {
                        import.Append( "import { " ).Append( e.Types ).Append( " } from " )
                              .AppendSourceString( e.LibraryName ).Append( ";" ).NewLine();
                    }
                }
                if( _importFiles != null )
                {
                    foreach( var e in _importFiles )
                    {
                        import.Append( "import { " ).Append( e.Types ).Append( " } from " )
                              .AppendSourceString( File.Folder.GetRelativePathTo( e.File.Folder ).AppendPart( e.File.Name.Remove( e.File.Name.Length - 3 ) ) )
                              .Append( ";" ).NewLine();
                    }
                }
                import.Build( b );
            }
            return base.Build( b );
        }

    }
}
