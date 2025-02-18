namespace CK.Core;

/// <summary>
/// A resource asset is a resource and a <see cref="ResourceOverrideKind"/>.
/// </summary>
/// <param name="Origin">The resource itself.</param>
/// <param name="Override">The override kind.</param>
public readonly record struct ResourceAsset( ResourceLocator Origin, ResourceOverrideKind Override );
