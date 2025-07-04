using System.Diagnostics.CodeAnalysis;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Models an imported name from a <see cref="ITSImportLine"/>.
/// </summary>
/// <param name="ExportedName">The actual name to import.</param>
/// <param name="ImportedName">Optional alias name for the exported name.</param>
public readonly record struct TSImportedName( string ExportedName, string? ImportedName )
{
    /// <summary>
    /// Gets whether the <see cref="ExportedName"/> is aliased: <see cref="ImportedName"/> is not null.
    /// </summary>
    [MemberNotNullWhen( true, nameof( ImportedName ) )]
    public bool IsAliased => ImportedName != null;

    /// <summary>
    /// Overridden to return "ExportedName as ImportedName" or "ExportedName".
    /// </summary>
    /// <returns>The import names.</returns>
    public override string ToString() => IsAliased ? $"{ExportedName} as {ImportedName}" : ExportedName;
}
