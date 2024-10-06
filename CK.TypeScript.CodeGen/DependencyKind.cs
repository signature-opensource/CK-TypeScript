namespace CK.TypeScript.CodeGen;

/// <summary>
/// Represent one of the dependencies list of the package.json.
/// </summary>
public enum DependencyKind
{
    /// <summary>
    /// The dependency is in the package.json devDependencies list.
    /// </summary>
    DevDependency = 0,

    /// <summary>
    /// The dependency is in the package.json dependencies list.
    /// </summary>
    Dependency = 1,

    /// <summary>
    /// The dependency is in the package.json peerDependencies list.
    /// </summary>
    PeerDependency = 2
}

/// <summary>
/// Extends <see cref="DependencyKind"/>.
/// </summary>
public static class DependencyKindExtensions
{
    static readonly string[] _names = new[] { "devDependencies", "dependencies", "peerDependencies" };

    /// <summary>
    /// Gets the package.json section name.
    /// </summary>
    /// <param name="kind">This kind.</param>
    /// <returns>The section name.</returns>
    public static string GetJsonSectionName( this DependencyKind kind ) => _names[(int)kind];
}
