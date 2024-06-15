using CK.Core;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Represent an external library that the generated code depend on.
    /// <para>
    /// LibraryImport are keyed by <see cref="Name"/> in <see cref="LibraryManager"/>. The <see cref="Version"/>
    /// is fixed by configuration xor is the minimal version bound of the code provided versions under the
    /// control of <see cref="LibraryManager.IgnoreVersionsBound"/>.
    /// </para>
    /// </summary>
    public sealed class LibraryImport
    {
        readonly string _name;
        SVersionBound _version;
        DependencyKind _dependencyKind;
        IReadOnlyCollection<LibraryImport> _impliedDependencies;
        bool _isUsed;

        LibraryImport( string name,
                       SVersionBound version,
                       DependencyKind dependencyKind,
                       IReadOnlyCollection<LibraryImport> impliedDependencies )
        {
            _name = name;
            _version = version;
            _dependencyKind = dependencyKind;
            _impliedDependencies = impliedDependencies;
        }

        internal static bool TryParseVersion( IActivityMonitor monitor, string name, string version, DependencyKind dependencyKind, out SVersionBound v )
        {
            // These are external libraries. Prerelease versions have not the same semantics as our in the npm
            // ecosystem. We use the mainstream semantics here.
            var parseResult = SVersionBound.NpmTryParse( version, includePrerelease: false );
            v = parseResult.Result;
            if( !parseResult.IsValid )
            {
                monitor.Error( $"Invalid version '{version}' for TypeScript library '{name}' ({dependencyKind}): {parseResult.Error}" );
                return false;
            }
            return true;
        }

        internal static LibraryImport Create( IActivityMonitor monitor, string name, SVersionBound v, DependencyKind dependencyKind, LibraryImport[] impliedDependencies )
        {
            if( impliedDependencies.GroupBy( d => d.Name ).Count() != impliedDependencies.Length )
            {
                var dup = impliedDependencies.Select( d => d.Name ).GroupBy( Util.FuncIdentity ).Where( d => d.Count() > 1 ).Select( d => d.Key );
                monitor.Warn( $"Duplicate found in implied TypeScript libraries of library '{name}': {dup.Concatenate()}." );
                impliedDependencies = impliedDependencies.Distinct().ToArray();
            }
            return new LibraryImport( name, v, dependencyKind, impliedDependencies );
        }

        /// <summary>
        /// Name of the package name, which will be the string put in the package.json.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Version of the package, which will be used in the package.json.
        /// This version can be fixed by the configuration. See <see cref="TypeScriptRoot.LibraryVersionConfiguration"/>.
        /// </summary>
        public SVersionBound Version => _version;

        /// <summary>
        /// Dependency kind of the package. Will be used to determine in which list
        /// of the packgage.json the dependency should appear.
        /// </summary>
        public DependencyKind DependencyKind => _dependencyKind;

        /// <summary>
        /// Gets a set of dependencies that must be available whenever this one is.
        /// </summary>
        public IReadOnlyCollection<LibraryImport> ImpliedDependencies => _impliedDependencies;

        /// <summary>
        /// Gets or sets whether this library is actually used: <see cref="ITSFileImportSection.EnsureImportFromLibrary(LibraryImport, string, string[])"/>
        /// sets it to true.
        /// <para>
        /// This trims the libraries: a library can be registered (thanks to <see cref="LibraryManager.RegisterLibrary"/>) but none of its types
        /// may be used. When a library is not used, it shouldn't appear in the package.json.
        /// For some libraries (typically <see cref="DependencyKind.DevDependency"/>), this can be set to true directly. Note that once
        /// set to true, it never transition back to false.
        /// </para>
        /// </summary>
        public bool IsUsed
        {
            get => _isUsed;
            set
            {
                if( !_isUsed && value )
                {
                    _isUsed = true;
                    foreach( var d in ImpliedDependencies )
                    {
                        d.IsUsed = true;
                    }
                }
            }
        }

        internal bool Update( IActivityMonitor monitor, SVersionBound newVersion, bool ignoreVersionsBound )
        {
            if( newVersion != _version )
            {
                SVersionBound newV;
                if( Version.Contains( newVersion ) ) newV = newVersion;
                else if( newVersion.Contains( Version ) ) newV = Version;
                else
                {
                    if( !ignoreVersionsBound )
                    {
                        monitor.Error( $"""
                                    TypeScript library '{_name}': incompatible versions detected between '{_version}' and '{newVersion}'.
                                    Set IgnoreVersionsBound to true to force the upgrade.
                                    """ );
                        return false;
                    }
                    newV = Version.Base > newVersion.Base ? Version : newVersion;
                    monitor.Warn( $"TypeScript library '{_name}': incompatible versions detected between '{_version}' and '{newVersion}'. IgnoreVersionsBound is true." );
                }
                monitor.Trace( $"TypeScript library '{_name}': version upgrade from '{_version}' to '{newV}'." );
                _version = newV;
            }
            return true;
        }

        internal void Update( DependencyKind kind )
        {
            if( kind > _dependencyKind ) _dependencyKind = kind;
        }

        internal void Update( IEnumerable<LibraryImport> implied )
        {
            if( implied.Any() )
            {
                _impliedDependencies = _impliedDependencies.Concat( implied ).Distinct().ToArray();
            }
        }

    }
}
