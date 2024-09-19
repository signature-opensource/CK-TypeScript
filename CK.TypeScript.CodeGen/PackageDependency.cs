using CK.Core;
using CSemVer;
using System.Diagnostics.CodeAnalysis;

namespace CK.TypeScript.CodeGen
{

    /// <summary>
    /// Captures a package dependency. This class is (surprisingly) mutable: the version and the kind can
    /// evolve.
    /// <para>
    /// The <see cref="TypeScriptRoot.Save(IActivityMonitor, TypeScriptFileSaveStrategy)"/> updates the
    /// <see cref="TypeScriptFileSaveStrategy.GeneratedDependencies"/> that can be altered before being
    /// used to create or update package.json files.
    /// </para>
    /// </summary>
    public sealed class PackageDependency
    {
        readonly string _name;
        SVersionBound _version;
        DependencyKind _dependencyKind;

        /// <summary>
        /// Initializes a new <see cref="PackageDependency"/>.
        /// </summary>
        /// <param name="name">The dependency name.</param>
        /// <param name="version">The version bound.</param>
        /// <param name="dependencyKind">The kind of dependency.</param>
        public PackageDependency( string name, SVersionBound version, DependencyKind dependencyKind )
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( name );
            _name = name;
            _version = version;
            _dependencyKind = dependencyKind;
        }

        /// <summary>
        /// Name of the package name, which will be the string put in the package.json.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets the version of the package, which will be used in the package.json.
        /// This version can be fixed by the configuration. See <see cref="TypeScriptRoot.LibraryVersionConfiguration"/>.
        /// <para>
        /// The <see cref="SVersionBound.None"/> is used to denote the special "workspace:*". See <see cref="IsWorkspaceDependency"/>.
        /// </para>
        /// <para>
        /// <see cref="SVersionBound.All"/> ("0.0.0-0"[None,CI]" or "&gt;=0.0.0-0" for npm) can be used to denote the "latest" version.
        /// See <see cref="IsLatestDependency"/>.
        /// </para>
        /// </summary>
        public SVersionBound Version => _version;

        /// <summary>
        /// Gets whether this dependency is "workspace:*" dependency.
        /// See https://yarnpkg.com/features/workspaces#cross-references).
        /// <para>
        /// This version is invalid (there's little chance that a 2147483647.2147483647.2147483647 versioned package exists one day).
        /// </para>
        /// </summary>
        public bool IsWorkspaceDependency => _version == SVersionBound.None;

        /// <summary>
        /// Gets whether this dependency has a <see cref="SVersionBound.All"/> (&gt;=0.0.0-0) version.
        /// This dependency should be installed via npm/yarn tooling so that its "current" version is installed and the package.json
        /// be updated to reflect the resolved version.
        /// <para>
        /// Note that using this "&gt;=0.0.0-0" in a package.json and running a "yarn install" will actually install the package but
        /// without reflecting the resolved version in the package.json.
        /// </para>
        /// </summary>
        public bool IsLatestDependency => _version == SVersionBound.All;

        /// <summary>
        /// Dependency kind of the package. Will be used to determine in which list
        /// of the packgage.json the dependency should appear.
        /// </summary>
        public DependencyKind DependencyKind => _dependencyKind;

        /// <summary>
        /// Gets the <see cref="Version"/> as npm version range.
        /// <para>
        /// This is either "workspace:*" or the <see cref="SVersionBound.ToNpmString()"/>.
        /// </para>
        /// </summary>
        public string NpmVersionRange => IsWorkspaceDependency ? $"workspace:*" : _version.ToNpmString();

        /// <summary>
        /// Sets the <see cref="DependencyKind"/> even if the new one is lower than the existing one.
        /// </summary>
        /// <param name="kind">The dependency kind to set.</param>
        public void UnconditionalSetDependencyKind( DependencyKind kind ) => _dependencyKind = kind;

        /// <summary>
        /// Sets the <see cref="Version"/> regardless of the current version.
        /// </summary>
        /// <param name="version">The version bound to set.</param>
        public void UnconditionalSetVersion( SVersionBound version ) => _version = version;

        public override string ToString() => $"{_dependencyKind}: {_name} {NpmVersionRange}";

        internal bool DoUpdate( SVersionBound newVersion, bool ignoreVersionsBound, [NotNullWhen(false)]out string? error, out string? warn )
        {
            Throw.DebugAssert( newVersion != _version );
            error = warn = null;
            SVersionBound newV;
            if( newVersion == SVersionBound.None )
            {
                warn = $"TypeScript library '{_name}' changed to a \"workspace:*\" dependency.";
                newV = newVersion;
            }
            else
            {
                if( _version.Contains( newVersion ) ) newV = newVersion;
                else if( newVersion.Contains( _version ) ) newV = _version;
                else
                {
                    if( !ignoreVersionsBound )
                    {
                        error = $"""
                            TypeScript library '{_name}': incompatible versions detected between '{_version}' and '{newVersion}'.
                            Set IgnoreVersionsBound to true to allow the upgrade.
                            """;
                        return false;
                    }
                    newV = _version.Base > newVersion.Base ? _version : newVersion;
                    warn = $"""
                            TypeScript library '{_name}': incompatible versions detected between '{_version}' and '{newVersion}'.
                            Ignored since IgnoreVersionsBound is true.
                            """;
                }
            }
            _version = newV;
            return true;
        }

        internal void Update( DependencyKind kind )
        {
            if( kind > _dependencyKind ) _dependencyKind = kind;
        }

    }
}
