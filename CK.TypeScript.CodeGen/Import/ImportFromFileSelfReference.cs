using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Empty object pattern that honors the invariant that at least FromLocalCkGen is true or FromLibrary or FromTypeScriptFile
/// are not null but doesn't keep the imports.
/// </summary>
sealed class ImportFromFileSelfReference : ITSImportLine
{
    readonly TypeScriptFileBase _self;

    public ImportFromFileSelfReference( TypeScriptFileBase self )
    {
        _self = self;
    }

    public bool FromLocalCkGen => false;

    public LibraryImport? FromLibrary => null;

    public TypeScriptFileBase? FromTypeScriptFile => _self;

    public string? DefaultImportSymbol => null;

    public IReadOnlyList<TSImportedName> ImportedNames => ImmutableArray<TSImportedName>.Empty;

    public void Add( string symbolNames )
    {
    }
}
