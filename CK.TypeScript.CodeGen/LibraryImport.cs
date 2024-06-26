using CK.Core;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Represent an external library that the generated code depends on.
    /// <para>
    /// LibraryImport are keyed by <see cref="Name"/> in <see cref="LibraryManager"/>. The <see cref="Version"/>
    /// is fixed by configuration xor is the minimal version bound of the code provided versions under the
    /// control of <see cref="LibraryManager.IgnoreVersionsBound"/>.
    /// </para>
    /// </summary>
    public sealed class LibraryImport
    {
        readonly PackageDependency _packageDependency;
        string _definitionSource;
        IReadOnlyCollection<LibraryImport> _impliedDependencies;
        bool _isUsed;

        LibraryImport( string name,
                       SVersionBound version,
                       DependencyKind dependencyKind,
                       IReadOnlyCollection<LibraryImport> impliedDependencies,
                       string definitionSource )
        {
            _packageDependency = new PackageDependency( name, version, dependencyKind );
            _impliedDependencies = impliedDependencies;
            _definitionSource = definitionSource;
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

        internal static LibraryImport Create( IActivityMonitor monitor,
                                              string name,
                                              SVersionBound v,
                                              DependencyKind dependencyKind,
                                              string definitionSource,
                                              LibraryImport[] impliedDependencies )
        {
            if( impliedDependencies.GroupBy( d => d.Name ).Count() != impliedDependencies.Length )
            {
                var dup = impliedDependencies.Select( d => d.Name ).GroupBy( Util.FuncIdentity ).Where( d => d.Count() > 1 ).Select( d => d.Key );
                monitor.Warn( $"""
                            Duplicate found in implied TypeScript libraries of library '{name}': {dup.Concatenate()}.
                            Source: {definitionSource}
                            """ );
                impliedDependencies = impliedDependencies.Distinct().ToArray();
            }
            return new LibraryImport( name, v, dependencyKind, impliedDependencies, definitionSource );
        }

        /// <summary>
        /// Name of the package name, which will be the string put in the package.json.
        /// </summary>
        public string Name => _packageDependency.Name;

        /// <summary>
        /// Version of the package, which will be used in the package.json.
        /// This version can be fixed by the configuration. See <see cref="TypeScriptRoot.LibraryVersionConfiguration"/>.
        /// </summary>
        public SVersionBound Version => _packageDependency.Version;

        /// <summary>
        /// Dependency kind of the package. Will be used to determine in which list
        /// of the packgage.json the dependency should appear.
        /// </summary>
        public DependencyKind DependencyKind => _packageDependency.DependencyKind;

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

        internal PackageDependency PackageDependency => _packageDependency;

        internal bool Update( IActivityMonitor monitor, SVersionBound newVersion, bool ignoreVersionsBound, string definitionSource )
        {
            if( newVersion != _packageDependency.Version )
            {
                var current = _packageDependency.Version;
                if( !_packageDependency.DoUpdate( newVersion, ignoreVersionsBound, out var error, out var warn ) )
                {
                    monitor.Error( AppendDefinitionSources( _definitionSource, definitionSource, error ) );
                    return false;
                }
                if( warn != null ) monitor.Warn( AppendDefinitionSources( _definitionSource, definitionSource, warn ) );
                if( _packageDependency.Version != current )
                {
                    monitor.Trace( $"TypeScript library '{_packageDependency.Name}': version upgrade from '{current}' to '{_packageDependency.Version}' (source: {definitionSource})." );
                    _definitionSource = definitionSource;
                }
            }
            return true;

            static string AppendDefinitionSources( string currentSource, string definitionSource, string s )
            {
                return $"""
                        {s}
                        Defining conficting sources are: '{currentSource}' and '{definitionSource}'. 
                        """;
            }
        }

        internal void Update( DependencyKind kind ) => _packageDependency.Update( kind );

        internal void Update( IEnumerable<LibraryImport> implied )
        {
            if( implied.Any() )
            {
                _impliedDependencies = _impliedDependencies.Concat( implied ).Distinct().ToArray();
            }
        }
    }
}
