namespace CK.StObj.TypeScript
{
    /// <summary>
    /// Represent one of the dependencies list of the package.json.
    /// </summary>
    public enum DependencyKind
    {
        /// <summary>
        /// The dependency will be put in the package.json devDependencies list.
        /// </summary>
        DevDependency,

        /// <summary>
        /// The dependency will be put in the package.json dependencies list.
        /// </summary>
        Dependency,

        /// <summary>
        /// The dependency will be put in the package.json peerDependencies list.
        /// </summary>
        PeerDependency
    }

}
