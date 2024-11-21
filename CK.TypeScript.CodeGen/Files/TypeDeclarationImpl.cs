using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Factorized implementation of declared only type.
/// </summary>
struct TypeDeclarationImpl
{
    List<ITSDeclaredFileType>? _declaredOnlyTypes;

    public readonly IEnumerable<ITSDeclaredFileType> AllTypes => _declaredOnlyTypes ?? Enumerable.Empty<ITSDeclaredFileType>();

    public ITSDeclaredFileType DeclareType( IMinimalTypeScriptFile declFile, string typeName, Action<ITSFileImportSection>? additionalImports, string? defaultValueSource )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
        _declaredOnlyTypes ??= new List<ITSDeclaredFileType>();
        var t = new TSDeclaredType( declFile, typeName, additionalImports, defaultValueSource );
        _declaredOnlyTypes.Add( t );
        return t;
    }

    // Pure TS type declaration without a specific TypePart in the file.
    public class TSDeclaredType : TSBasicType, ITSDeclaredFileType
    {
        readonly IMinimalTypeScriptFile _file;

        public TSDeclaredType( IMinimalTypeScriptFile file, string typeName, Action<ITSFileImportSection>? additionalImports, string? defaultValueSource )
            : base( file.Root.TSTypes, typeName, additionalImports, defaultValueSource )
        {
            _file = file;
        }

        public override IMinimalTypeScriptFile File => _file;

        public override void EnsureRequiredImports( ITSFileImportSection section )
        {
            base.EnsureRequiredImports( section );
            section.ImportFromFile( _file, TypeName );
        }
    }
}



