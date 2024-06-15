using CK.Core;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Centrally manages <see cref="LibraryImport"/>.
    /// </summary>
    public sealed class LibraryManager
    {
        readonly Dictionary<string, LibraryImport> _libraries;
        readonly ImmutableDictionary<string, SVersionBound> _libVersionsConfig;
        readonly string _decimalLibraryName;
        readonly bool _ignoreVersionsBound;

        internal LibraryManager( ImmutableDictionary<string, SVersionBound>? libVersionsConfig, string decimalLibraryName, bool ignoreVersionsBound )
        {
            _libraries = new Dictionary<string, LibraryImport>( StringComparer.OrdinalIgnoreCase );
            _libVersionsConfig = libVersionsConfig ?? ImmutableDictionary<string,SVersionBound>.Empty;
            _decimalLibraryName = decimalLibraryName;
            _ignoreVersionsBound = ignoreVersionsBound;
        }

        /// <summary>
        /// Imported external library declared by the generated code. Versions are under control of the <see cref="LibraryVersionConfiguration"/>.
        /// </summary>
        public IReadOnlyDictionary<string, LibraryImport> LibraryImports => _libraries;

        /// <summary>
        /// Gets the library to use for <see cref="decimal"/>.
        /// </summary>
        public string DecimalLibraryName => _decimalLibraryName;

        /// <summary>
        /// Gets whether <see cref="SVersionBound"/> are honored or not when different versions of
        /// the same library are declared.
        /// </summary>
        public bool IgnoreVersionsBound => _ignoreVersionsBound;

        /// <summary>
        /// Gets the configured versions for external npm packages. These configurations take precedence over the
        /// library versions declared by code via <see cref="LibraryManager.RegisterLibrary"/>.
        /// <para>
        /// Only libraries that are used by code will be used from this configuration.
        /// </para>
        /// </summary>
        public IReadOnlyDictionary<string, SVersionBound> LibraryVersionConfiguration => _libVersionsConfig;

        /// <summary>
        /// Tries to register an external library.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="name">
        /// The library name. Must not be empty but can be null: the code wants the library version
        /// to be configured.
        /// </param>
        /// <param name="dependencyKind">
        /// The kind of dependencies. This always boost an existing kind: <see cref="DependencyKind.PeerDependency"/>
        /// always wins over <see cref="DependencyKind.Dependency"/> that always wins over <see cref="DependencyKind.DevDependency"/>.
        /// </param>
        /// <param name="impliedDependencies">Optional libraries that must also be imported when this one is imported in a <see cref="ITSFileImportSection"/>.</param>
        /// <returns>The library imort or null on error.</returns>
        public LibraryImport? RegisterLibrary( IActivityMonitor monitor,
                                               string name,
                                               string? version,
                                               DependencyKind dependencyKind,
                                               params LibraryImport[] impliedDependencies )
        {
            if( string.IsNullOrWhiteSpace( name ) )
            {
                monitor.Error( $"Invalid TypeScript library name '{name}'." );
                return null;
            }
            if( version == null )
            {
                return RegisterNoVersionLibrary( monitor, name, dependencyKind, impliedDependencies );
            }
            if( !LibraryImport.TryParseVersion( monitor, name, version, dependencyKind, out var v ) )
            {
                return null;
            }
            bool isConfigured = _libVersionsConfig.TryGetValue( name, out var configured );
            if( isConfigured && v != configured )
            {
                monitor.Info( $"TypeScript library '{name}' will use the configured version '{configured}'. Ignoring code provided version '{v}'." );
                v = configured;
            }
            if( !_libraries.TryGetValue( name, out var lib ) )
            {
                // New library: creating it (no log).
                lib = LibraryImport.Create( monitor, name, v, dependencyKind, impliedDependencies );
                _libraries.Add( name, lib );
            }
            else 
            {
                // It exists. If it comes from the configuration, don't try to upgrade the version
                // but always boost the dependency kind and merger the implied dependencies.
                if( !isConfigured && !lib.Update( monitor, v, _ignoreVersionsBound ) )
                {
                    return null;
                }
                lib.Update( dependencyKind );
                lib.Update( impliedDependencies );
            }
            return lib;
        }

        LibraryImport? RegisterNoVersionLibrary( IActivityMonitor monitor, string name, DependencyKind dependencyKind, LibraryImport[] impliedDependencies )
        {
            // Allow the library to be registered by another package before screaming.
            // This is not perfect: this depends on the execution order but this can avoid
            // the failure and a configuration update.
            if( _libraries.TryGetValue( name, out var lib ) )
            {
                lib.Update( dependencyKind );
                lib.Update( impliedDependencies );
                return lib;
            }
            if( !_libVersionsConfig.TryGetValue( name, out var configured ) )
            {
                monitor.Error( $"TypeScript library '{name}' requires its version to be configured." );
                return null;
            }
            lib = LibraryImport.Create( monitor, name, configured, dependencyKind, impliedDependencies );
            _libraries.Add( name, lib );
            return lib;
        }

        internal LibraryImport RegisterDefaultOrConfiguredDecimalLibrary( IActivityMonitor monitor )
        {
            var knownLibVersion = _decimalLibraryName == TypeScriptRoot.DecimalJSLight
                                    ? TypeScriptRoot.DecimalJSLightVersion
                                    : _decimalLibraryName == TypeScriptRoot.DecimalJS
                                        ? TypeScriptRoot.DecimalJSVersion
                                        : null;
            var lib = RegisterLibrary( monitor, _decimalLibraryName, knownLibVersion, DependencyKind.Dependency );
            Throw.DebugAssert( lib != null );
            return lib;
        }

        internal LibraryImport RegisterLuxonLibrary( IActivityMonitor monitor )
        {
            var luxonTypesLib = RegisterLibrary( monitor, "@types/luxon", TypeScriptRoot.LuxonTypesVersion, DependencyKind.DevDependency );
            Throw.DebugAssert( luxonTypesLib != null );
            var luxonLib = RegisterLibrary( monitor, "luxon", TypeScriptRoot.LuxonVersion, DependencyKind.Dependency, luxonTypesLib );
            Throw.DebugAssert( luxonLib != null );
            return luxonLib;
        }
    }
}
