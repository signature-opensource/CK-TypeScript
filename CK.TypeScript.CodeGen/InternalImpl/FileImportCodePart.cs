using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{

    class FileImportCodePart : BaseCodeWriter, ITSFileImportSection
    {
        List<(TypeScriptFile File, List<string> Types)>? _imports;
        int _importCount;

        public FileImportCodePart( TypeScriptFile f )
            : base( f )
        {
        }

        public TSFileImportedSection EnsureImport( string typeName, TypeScriptFile file )
        {
            if( file == null ) throw new ArgumentNullException( nameof( file ) );
            if( file != File )
            {
                var types = EnsureFile( file );
                if( !types.Contains( typeName ) )
                {
                    ++_importCount;
                    types.Add( typeName );
                }
            }
            return new TSFileImportedSection( File, file );
        }

        public int ImportCount => _importCount;


        internal override SmarterStringBuilder Build( SmarterStringBuilder b )
        {
            if( _imports != null )
            {
                var import = new BaseCodeWriter( File );
                foreach( var e in _imports )
                {
                    import.Append( "import { " ).Append( e.Types ).Append( " } from " )
                          .AppendSourceString( File.Folder.GetRelativePathTo( e.File.Folder ).AppendPart( e.File.Name.Remove( e.File.Name.Length - 3 ) ) )
                          .Append( ";" ).NewLine();
                }
                import.Build( b );
            }
            return base.Build( b );
        }

        List<string> EnsureFile( TypeScriptFile file )
        {
            if( _imports == null ) _imports = new List<(TypeScriptFile, List<string>)>();
            foreach( var e in _imports )
            {
                if( e.File == file ) return e.Types;
            }
            var types = new List<string>();
            _imports.Add( (file, types) );
            return types;
        }
    }
}
