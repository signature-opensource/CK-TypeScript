namespace CK.StObj.TypeScript.Tests.CrisLike;

/// <summary>
/// Simple data record. Compatible with a IPoco field (no mutable reference).
/// </summary>
/// <param name="Value">The data value.</param>
/// <param name="Name">The data name.</param>
[TypeScript( SameFolderAs = typeof( ICommandAbs ) )]
public record struct NamedRecord( int Value, string Name );
