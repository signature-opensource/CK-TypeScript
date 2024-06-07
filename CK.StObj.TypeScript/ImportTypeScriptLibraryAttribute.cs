using CK.Setup;
using System;
using System.Collections.Immutable;

namespace CK.StObj.TypeScript
{
    /// <summary>
    /// Decorates a class (that can be static) to specify a library that will be
    /// included in the /ck-gen/package.json file.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ImportTypeScriptLibraryAttribute : ContextBoundDelegationAttribute
    {
        /// <summary>
        /// Initializes a new ImportTypeScriptLibrary attribute.
        /// </summary>
        /// <param name="import">One or more library to import.</param>
        public ImportTypeScriptLibraryAttribute( string name, string version, DependencyKind dependencyKind )
            : base( "CK.StObj.TypeScript.Engine.ImportTypeScriptLibraryAttributeImpl, CK.StObj.TypeScript.Engine" )
        {
            Name = name;
            Version = version;
            DependencyKind = dependencyKind;
        }

        /// <summary>
        /// Gets the name of the package name, which will be the string put in the package.json.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the version of the package, which will be used in the package.json.
        /// This version can be overridden by the configuration. See <see cref="TypeScriptAspectConfiguration.LibraryVersions"/>.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Dependency kind of the package. Will be used to determine in which list
        /// of the packgage.json the dependency should appear.
        /// </summary>
        public DependencyKind DependencyKind { get; }
    }

}
