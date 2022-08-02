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


        public ITSFileImportSection EnsureImportFromLibrary( LibraryImport libraryImport, string typeName, params string[] typeNames )
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( libraryImport.Name );
            Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
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
            Add( types, typeName );
            foreach( var t in typeNames )
            {
                Add( types, t );
            }

            void Add( List<string> types, string typeName )
            {
                if( !types.Contains( typeName ) )
                {
                    ++_importCount;
                    types.Add( typeName );
                }
            }

            return types;
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
