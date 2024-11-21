using CK.Core;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Centrally manages <see cref="LibraryImport"/>.
/// </summary>
public sealed class LibraryManager
{
    /// <summary>
    /// Default "@types/luxon" package version. A configured version (in <see cref="LibraryVersionConfiguration"/>)
    /// overrides this default.
    /// </summary>
    public const string LuxonTypesVersion = "^3.4.2";

    /// <summary>
    /// Default "luxon" package version. A configured version (in <see cref="LibraryVersionConfiguration"/>)
    /// overrides this default.
    /// </summary>
    public const string LuxonVersion = "^3.5.0";

    /// <summary>
    /// See https://mikemcl.github.io/decimal.js-light/.
    /// </summary>
    public const string DecimalJSLight = "decimal.js-light";

    /// <summary>
    /// Default "decimal.js-light" package version. A configured version (in <see cref="LibraryVersionConfiguration"/>)
    /// overrides this default.
    /// </summary>
    public const string DecimalJSLightVersion = "2.5.1";

    /// <summary>
    /// See https://mikemcl.github.io/decimal.js/.
    /// </summary>
    public const string DecimalJS = "decimal.js";

    /// <summary>
    /// Default "decimal.js" package version. A configured version (in <see cref="LibraryVersionConfiguration"/>)
    /// overrides this default.
    /// </summary>
    public const string DecimalJSVersion = "10.4.3";

    readonly Dictionary<string, LibraryImport> _libraries;
    readonly ImmutableDictionary<string, SVersionBound> _libVersionsConfig;
    readonly string _decimalLibraryName;
    readonly bool _ignoreVersionsBound;

    internal LibraryManager( ImmutableDictionary<string, SVersionBound>? libVersionsConfig, string decimalLibraryName, bool ignoreVersionsBound )
    {
        _libraries = new Dictionary<string, LibraryImport>( StringComparer.OrdinalIgnoreCase );
        _libVersionsConfig = libVersionsConfig ?? ImmutableDictionary<string, SVersionBound>.Empty;
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
    /// Gets whether when code declares multiple version bounds for the same library, version compatibility must be enforced or not.
    /// <para>
    /// When false (the default), if a package wants "axios": "^0.28.0" (in <see cref="SVersionBound"/> semantics: "0.28.0[LockMajor,Stable]")
    /// and another one wants "&gt;=1.7.2" (that is "1.7.2[Stable]"), this will fail. 
    /// </para>
    /// <para>
    /// When set to true, the greatest <see cref="SVersionBound.Base"/> wins: "1.7.2[Stable]" will be selected.
    /// </para>
    /// </summary>
    public bool IgnoreVersionsBound => _ignoreVersionsBound;

    /// <summary>
    /// Gets the configured versions for external npm packages. These configurations take precedence over the
    /// library versions declared by code via <see cref="RegisterLibrary(IActivityMonitor, string, string?, DependencyKind, string?, LibraryImport[])"/>.
    /// <para>
    /// These configurations are ignored for libraries that are not used by the code (no <see cref="ITSFileImportSection.EnsureImportFromLibrary(LibraryImport, string, string[])"/>
    /// are made).
    /// </para>
    /// </summary>
    public IReadOnlyDictionary<string, SVersionBound> LibraryVersionConfiguration => _libVersionsConfig;

    /// <summary>
    /// Creates a <see cref="PackageDependency"/> under control of the <paramref name="libVersionsConfig"/>.
    /// </summary>
    /// <param name="monitor">The monitpr to use.</param>
    /// <param name="libVersionsConfig">The configured package versions.</param>
    /// <param name="name">The package name.</param>
    /// <param name="versionBound">
    /// When null, the library version must be registered in the <see cref="LibraryVersionConfiguration"/>.
    /// This allows code to require an explicitly configured version for a library.
    /// </param>
    /// <param name="dependencyKind">The dependency kind.</param>
    /// <param name="packageDefinitionSource">The source definition of the package. Defaults to "&lt;no source&gt;".</param>
    /// <returns>The package or null on error.</returns>
    public static PackageDependency? CreatePackageDependency( IActivityMonitor monitor,
                                                              ImmutableDictionary<string, SVersionBound> libVersionsConfig,
                                                              string name,
                                                              SVersionBound? versionBound,
                                                              DependencyKind dependencyKind,
                                                              string? packageDefinitionSource )
    {
        packageDefinitionSource ??= "<no source>";
        bool isConfigured;
        SVersionBound configured;
        if( versionBound is null )
        {
            if( !libVersionsConfig.TryGetValue( name, out configured ) )
            {
                monitor.Error( $"TypeScript library '{name}' requires its version to be configured by '{packageDefinitionSource}'." );
                return null;
            }
            versionBound = configured;
            isConfigured = true;
        }
        else
        {
            isConfigured = libVersionsConfig.TryGetValue( name, out configured );
        }
        if( isConfigured && versionBound != configured )
        {
            monitor.Info( $"TypeScript library '{name}' will use the configured version '{configured}'. Ignoring version '{versionBound}' from '{packageDefinitionSource}'." );
            versionBound = configured;
        }
        return new PackageDependency( name, versionBound.Value, dependencyKind, isConfigured ? PackageDependency.ConfigurationSourceName : packageDefinitionSource );
    }

    /// <summary>
    /// Tries to parse a Npm version string. If the version cannot be parsed an error (with a detailed reason)
    /// is logged in the <paramref name="monitor"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="name">The dependency name.</param>
    /// <param name="version">The version to parse.</param>
    /// <param name="dependencyKind">The dependency kind of the </param>
    /// <param name="v">The resulting version bound on success.</param>
    /// <returns></returns>
    public static bool TryParseVersionBound( IActivityMonitor monitor, string name, string version, DependencyKind dependencyKind, out SVersionBound v )
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
        // Normalize "*" and "" to ">=0.0.0-0".
        v = v.NormalizeNpmVersionBoundAll();
        return true;
    }

    /// <summary>
    /// Tries to register an external library with a version bound.
    /// To register library without any version constraint, use <see cref="RegisterLibrary(IActivityMonitor, string, DependencyKind, string?, LibraryImport[])"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="name">The library name.</param>
    /// <param name="versionBound">
    /// When null, the library version must be registered in the <see cref="LibraryVersionConfiguration"/>.
    /// This allows code to require an explicitly configured version for a library.
    /// </param>
    /// <param name="dependencyKind">
    /// The kind of dependencies. This always boost an existing kind: <see cref="DependencyKind.PeerDependency"/>
    /// always wins over <see cref="DependencyKind.Dependency"/> that always wins over <see cref="DependencyKind.DevDependency"/>.
    /// </param>
    /// <param name="definitionSource">Definition of the caller that register this library (used to log info, errors or warnings). Defaults to "&lt;no source&gt;".</param>
    /// <param name="impliedDependencies">Optional libraries that must also be imported when this one is imported in a <see cref="ITSFileImportSection"/>.</param>
    /// <returns>The library import or null on error.</returns>
    public LibraryImport? RegisterLibrary( IActivityMonitor monitor,
                                           string name,
                                           string? versionBound,
                                           DependencyKind dependencyKind,
                                           [CallerFilePath] string? definitionSource = null,
                                           params LibraryImport[] impliedDependencies )
    {
        definitionSource ??= "<no source>";
        if( string.IsNullOrWhiteSpace( name ) )
        {
            monitor.Error( $"Invalid TypeScript library name '{name}' from '{definitionSource}'." );
            return null;
        }
        SVersionBound? v;
        if( versionBound == null )
        {
            v = null;
        }
        else
        {
            if( !TryParseVersionBound( monitor, name, versionBound, dependencyKind, out var vParsed ) )
            {
                return null;
            }
            v = vParsed;
        }
        var p = CreatePackageDependency( monitor, _libVersionsConfig, name, v, dependencyKind, definitionSource );
        if( p == null ) return null;

        impliedDependencies = WarnOnDuplicateImpliedDependencies( monitor, name, definitionSource, impliedDependencies );
        if( _libraries.TryGetValue( name, out var lib ) )
        {
            // It exists: use the central Update.
            if( !lib.PackageDependency.Update( monitor, p, _ignoreVersionsBound, LogLevel.Trace ) )
            {
                return null;
            }
            // Don't forget to merge the implied dependencies.
            lib.Update( impliedDependencies );
        }
        else
        {
            lib = new LibraryImport( p, impliedDependencies );
            _libraries.Add( name, lib );
        }
        return lib;
    }

    /// <summary>
    /// Registers an external library without any version constraint.
    /// This uses the ">=0.0.0-0" version bound (that is <see cref="SVersionBound.All"/>), the the "latest" npm version
    /// available should ultimately be installed or the library's version is configured by any other participant.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="name">The library name.</param>
    /// <param name="dependencyKind">
    /// The kind of dependencies. This always boost an existing kind: <see cref="DependencyKind.PeerDependency"/>
    /// always wins over <see cref="DependencyKind.Dependency"/> that always wins over <see cref="DependencyKind.DevDependency"/>.
    /// </param>
    /// <param name="definitionSource">Definition of the caller that register this library (used to log info, errors or warnings). Defaults to "&lt;no source&gt;".</param>
    /// <param name="impliedDependencies">Optional libraries that must also be imported when this one is imported in a <see cref="ITSFileImportSection"/>.</param>
    /// <returns>The library import.</returns>
    public LibraryImport RegisterLibrary( IActivityMonitor monitor,
                                          string name,
                                          DependencyKind dependencyKind,
                                          [CallerFilePath] string? definitionSource = null,
                                          params LibraryImport[] impliedDependencies )
    {
        definitionSource ??= "<no source>";
        impliedDependencies = WarnOnDuplicateImpliedDependencies( monitor, name, definitionSource, impliedDependencies );
        // If the library is already registered, simply updates its kind and implied dependencies.
        if( _libraries.TryGetValue( name, out var lib ) )
        {
            lib.PackageDependency.Update( monitor, LogLevel.Trace, dependencyKind, definitionSource );
            lib.Update( impliedDependencies );
            return lib;
        }
        // Consider the configured version if any.
        if( _libVersionsConfig.TryGetValue( name, out var configured ) )
        {
            monitor.Info( $"TypeScript library '{name}' will use the configured version '{configured}'." );
            definitionSource = PackageDependency.ConfigurationSourceName;
        }
        else
        {
            configured = SVersionBound.All;
        }
        var p = new PackageDependency( name, configured, dependencyKind, definitionSource );
        lib = new LibraryImport( p, impliedDependencies );
        _libraries.Add( name, lib );
        return lib;
    }

    static LibraryImport[] WarnOnDuplicateImpliedDependencies( IActivityMonitor monitor, string name, string definitionSource, LibraryImport[] impliedDependencies )
    {
        if( impliedDependencies.Length > 0 )
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
        }
        return impliedDependencies;
    }

    internal LibraryImport RegisterDefaultOrConfiguredDecimalLibrary( IActivityMonitor monitor )
    {
        var knownLibVersion = _decimalLibraryName == DecimalJSLight
                                ? DecimalJSLightVersion
                                : _decimalLibraryName == DecimalJS
                                    ? DecimalJSVersion
                                    : null;
        var lib = RegisterLibrary( monitor, _decimalLibraryName, knownLibVersion, DependencyKind.Dependency, "CK.TypeScript.CodeGen.LibraryManager" );
        Throw.DebugAssert( lib != null );
        return lib;
    }

    internal LibraryImport RegisterLuxonLibrary( IActivityMonitor monitor )
    {
        var luxonTypesLib = RegisterLibrary( monitor,
                                             "@types/luxon",
                                             LuxonTypesVersion,
                                             DependencyKind.DevDependency,
                                             "CK.TypeScript.CodeGen.LibraryManager" );
        Throw.DebugAssert( luxonTypesLib != null );
        var luxonLib = RegisterLibrary( monitor,
                                        "luxon",
                                        LuxonVersion,
                                        DependencyKind.Dependency,
                                        "CK.TypeScript.CodeGen.LibraryManager",
                                        luxonTypesLib );
        Throw.DebugAssert( luxonLib != null );
        return luxonLib;
    }
}
