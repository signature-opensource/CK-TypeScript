namespace CK.Less.Transform;

/// <summary>
/// Models the data for <see cref="EnsureImportStatement.EnsureOrderedImports"/>.
/// </summary>
/// <param name="Include">The import keywords that must appear.</param>
/// <param name="Exclude">The import keywords that must not appear: take precedence over <paramref name="Include"/>.</param>
/// <param name="ImportPath">Import path.</param>
public sealed record EnsureImportLine( ImportKeyword Include, ImportKeyword Exclude, string ImportPath );
