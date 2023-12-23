using CK.Core;
using System.Collections.Generic;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Represent an external library that the generated code depend on.
    /// </summary>
    public readonly struct LibraryImport
    {
        /// <summary>
        /// Initializes a new <see cref="LibraryImport"/>.
        /// </summary>
        /// <param name="name">The library <see cref="Name"/>.</param>
        /// <param name="version">The <see cref="Version"/> to import.</param>
        /// <param name="dependencyKind">The <see cref="DependencyKind"/>. </param>
        /// <param name="impliedDependencies">Optional set of dependencies that must be added whenever this one is added.</param>
        public LibraryImport( string name, string version, DependencyKind dependencyKind, params LibraryImport[] impliedDependencies )
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( name );
            Throw.CheckNotNullOrWhiteSpaceArgument( version );
            Name = name;
            Version = version;
            DependencyKind = dependencyKind;
            ImpliedDependencies = impliedDependencies;
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
        /// Dependency kind of the package. Will be used to determine in which list
        /// of the packgage.json the dependency should appear.
        /// </summary>
        public DependencyKind DependencyKind { get; }

        /// <summary>
        /// Gets a set of dependencies that must be available whenever this one is.
        /// </summary>
        public IReadOnlyCollection<LibraryImport> ImpliedDependencies { get; }

    }
}
