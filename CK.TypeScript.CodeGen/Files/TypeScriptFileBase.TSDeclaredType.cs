using System;
using System.IO;

namespace CK.TypeScript.CodeGen;

public abstract partial class TypeScriptFileBase
{
    // Pure TS type declaration without a specific TypePart in the file.
    private protected class TSDeclaredType : TSBasicType, ITSDeclaredFileType
    {
        readonly TypeScriptFileBase _file;
        string? _importPath;

        public TSDeclaredType( TypeScriptFileBase file,
                               string typeName,
                               Action<ITSFileImportSection>? additionalImports,
                               string? defaultValueSource )
            : base( file.Root.TSTypes, typeName, additionalImports, defaultValueSource )
        {
            _file = file;
            _file.Folder.SetHasExportedSymbol();
        }

        public override TypeScriptFileBase File => _file;

        public string ImportPath => _importPath ??= _file.Folder.Path + Path.GetFileNameWithoutExtension( _file.Name );

        public override void EnsureRequiredImports( ITSFileImportSection section )
        {
            base.EnsureRequiredImports( section );
            section.ImportFromFile( _file, TypeName );
        }
    }

}

