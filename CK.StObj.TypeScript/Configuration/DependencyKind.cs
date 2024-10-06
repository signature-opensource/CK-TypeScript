namespace CK.StObj.TypeScript;

/// <summary>
/// Represent one of the dependencies list of the package.json.
/// </summary>
public enum DependencyKind
{
    /// <summary>
    /// The dependency will be put in the package.json <c>"devDependencies"</c> list.
    /// </summary>
    DevDependency,

    /// <summary>
    /// The dependency will be put in the package.json <c>"dependencies"</c> list.
    /// </summary>
    Dependency,

    /// <summary>
    /// The dependency will be put in both <c>"peerDependencies"</c> and <c>"devDependencies"</c> lists.
    /// This trick simulates a transitive dependency.
    /// </summary>
    PeerDependency
}
