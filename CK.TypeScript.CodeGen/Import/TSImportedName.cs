using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Models an imported name from a <see cref="ITSImportLine"/>.
/// </summary>
public readonly record struct TSImportedName( string ExportedName, string? ImportedName )
{
    /// <summary>
    /// Gets whether the <see cref="ExportedName"/> is aliased: <see cref="ImportedName"/> is not null.
    /// </summary>
    [MemberNotNullWhen( true, nameof( ImportedName ) )]
    public bool IsAliased => ImportedName != null;

    /// <summary>
    /// Overridden to return the <see cref="ImportedName"/> or the <see cref="ExportedName"/> if it is not aliased.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => ImportedName ?? ExportedName;
}
