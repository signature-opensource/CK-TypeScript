using CK.EmbeddedResources;
using System;
using System.Collections.Generic;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// <see cref="BaseFile.Extension"/> is ".ts".
/// </summary>
public sealed class ResourceTypeScriptFile : ResourceTextFileBase, IMinimalTypeScriptFile
{
    TypeDeclarationImpl _declared;

    internal ResourceTypeScriptFile( TypeScriptFolder folder, string name, in ResourceLocator locator )
        : base( folder, name, in locator )
    {
    }

    /// <inheritdoc />
    public IEnumerable<ITSDeclaredFileType> AllTypes => _declared.AllTypes;

    /// <inheritdoc />
    public ITSDeclaredFileType DeclareType( string typeName, Action<ITSFileImportSection>? additionalImports = null, string? defaultValueSource = null )
    {
        return _declared.DeclareType( this, typeName, additionalImports, defaultValueSource );
    }
}


