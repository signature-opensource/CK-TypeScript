namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Represent an external library that the generated code depend on.
    /// </summary>
    public readonly struct LibraryImport
    {
        /// <summary>
        /// Construct a <see cref="LibraryImport"/>.
        /// </summary>
        /// <param name="name">Sets <see cref="Name"/>.</param>
        /// <param name="version">Sets <see cref="Version"/>.</param>
        /// <param name="dependencyKind"> Sets <see cref="DependencyKind"/>. </param>
        public LibraryImport( string name, string version, DependencyKind dependencyKind )
        {
            Name = name;
            Version = version;
            DependencyKind = dependencyKind;
        }

        /// <summary>
        /// Name of the package name, which will be the string put in the package.json.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Version of the package, which will be used in the package.json.
        /// </summary>
        public string Version { get; }
        /// <summary>
        /// Dependency Kind of the package, which will be used to determine in which list of the packgage.json the dependency should go.
        /// </summary>
        public DependencyKind DependencyKind { get; }
    }
}
