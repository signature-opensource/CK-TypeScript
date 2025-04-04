using System;

namespace CK.TypeScript.CodeGen;

public abstract partial class TypeScriptFileBase
{
    // Pure TS type declaration without a specific TypePart in the file.
    protected class TSDeclaredType : TSBasicType, ITSDeclaredFileType
    {
        readonly TypeScriptFileBase _file;

        public TSDeclaredType( TypeScriptFileBase file, string typeName, Action<ITSFileImportSection>? additionalImports, string? defaultValueSource )
            : base( file.Root.TSTypes, typeName, additionalImports, defaultValueSource )
        {
            _file = file;
        }

        public override TypeScriptFileBase File => _file;

        public override void EnsureRequiredImports( ITSFileImportSection section )
        {
            base.EnsureRequiredImports( section );
            section.ImportFromFile( _file, TypeName );
        }
    }

}

