using CK.Core;
using CSemVer;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Captures a package dependency. This class is (surprisingly) mutable: the version and the kind can
/// evolve.
/// </summary>
public sealed class PackageDependency
{
    readonly string _name;
    SVersionBound _version;
    DependencyKind _dependencyKind;
    string _definitionSource;

    /// <summary>
    /// Name of the definitive <see cref="DefinitionSource"/>.
    /// Only the <see cref="LibraryManager"/> should use this for its <see cref="LibraryManager.LibraryVersionConfiguration"/>.
    /// </summary>
    public const string ConfigurationSourceName = "Configuration";

    /// <summary>
    /// Initializes a new <see cref="PackageDependency"/>.
    /// </summary>
    /// <param name="name">The dependency name.</param>
    /// <param name="version">The version bound.</param>
    /// <param name="dependencyKind">The kind of dependency.</param>
    /// <param name="definitionSource">The dependency source definition.</param>
    public PackageDependency( string name, SVersionBound version, DependencyKind dependencyKind, string definitionSource )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( name );
        Throw.CheckNotNullOrWhiteSpaceArgument( definitionSource );
        _name = name;
        _version = version;
        _dependencyKind = dependencyKind;
        _definitionSource = definitionSource;
    }

    /// <summary>
    /// Name of the package name, which will be the string put in the package.json.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets the version of the package, which will be used in the package.json.
    /// This version can be fixed by the configuration. See <see cref="LibraryManager.LibraryVersionConfiguration"/>.
    /// <para>
    /// The <see cref="SVersionBound.None"/> is used to denote the special "workspace:*". See <see cref="IsWorkspaceDependency"/>.
    /// </para>
    /// <para>
    /// <see cref="SVersionBound.All"/> ("0.0.0-0"[None,CI]" or "&gt;=0.0.0-0", "*" or "" for npm) can be used to denote the "latest" version.
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
    /// Gets the source definition.
    /// </summary>
    public string DefinitionSource => _definitionSource;

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

    /// <summary>
    /// Overridden to return the kind, name and the <see cref="NpmVersionRange"/>.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => $"{_dependencyKind}: {_name} {NpmVersionRange}";

    /// <summary>
    /// Updates this dependency from another one that must have the same <see cref="Name"/>.
    /// <para>
    /// This always fail if this <see cref="IsWorkspaceDependency"/> is true (but the other can be a workspace dependency:
    /// this one will become a workspace dependency and a waning will be logged).
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="other">The package dependency to merge with this one.</param>
    /// <param name="ignoreVersionsBound">True to skip version bound check.</param>
    /// <param name="detailedLogLevel">By default upgrades of version or kind are not logged.</param>
    /// <param name="logWarn">
    /// By default logs the warning of bound check failures (when <paramref name="ignoreVersionsBound"/> is true) and when
    /// the other is a <see cref="IsWorkspaceDependency"/>.
    /// </param>
    /// <returns>True on success, false on error.</returns>
    public bool Update( IActivityMonitor monitor,
                        PackageDependency other,
                        bool ignoreVersionsBound,
                        LogLevel detailedLogLevel = LogLevel.None,
                        bool logWarn = true )
    {
        if( _version == SVersionBound.None )
        {
            monitor.Error( $"TypeScript library '{_name}' is a Workspace dependency. It cannot be updated." );
            return false;
        }
        if( other._version != SVersionBound.None )
        {
            Update( monitor, detailedLogLevel, other._dependencyKind, other._definitionSource );
        }
        if( _version != other._version )
        {
            if( _definitionSource == ConfigurationSourceName )
            {
                monitor.Log( detailedLogLevel, $"TypeScript library '{_name}' version comes from the configuration. Ignoring '{other._version}' from '{other._definitionSource}'." );
                return true;
            }
            if( other._definitionSource == ConfigurationSourceName )
            {
                monitor.Log( detailedLogLevel, $"TypeScript library '{_name}': version is now the configured one '{other._version}'." );
                _version = other._version;
                _definitionSource = ConfigurationSourceName;
                return true;
            }
            if( other._version == SVersionBound.None )
            {
                if( logWarn )
                {
                    monitor.Warn( $"TypeScript library '{_name}' changed to a \"workspace:*\" dependency from '{other._definitionSource}'." );
                }
                _definitionSource = other._definitionSource;
                _version = SVersionBound.None;
            }
            else
            {
                if( !MergeVersionBound( monitor,
                                        $"TypeScript library '{_name}'",
                                        ref _version,
                                        ref _definitionSource,
                                        other._version,
                                        other._definitionSource,
                                        ignoreVersionsBound,
                                        detailedLogLevel,
                                        logWarn,
                                        out _ ) )
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Merges two version bounds, the way the versions of the libraries are merged: when one bound contains the other,
    /// the more specific one wins; bounds that don't overlap are an error unless <paramref name="ignoreVersionsBound"/>
    /// is true (then the greater one wins, with a warning).
    /// <para>
    /// This is used by <see cref="Update(IActivityMonitor, PackageDependency, bool, LogLevel, bool)"/> and by the other
    /// versioned declarations of the packages (the Yarn package extensions and resolutions).
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="subject">The subject of the log messages ("TypeScript library 'xxx'").</param>
    /// <param name="version">The current version, updated with the merged one.</param>
    /// <param name="definitionSource">The source of the current version, updated with the source of the merged one.</param>
    /// <param name="otherVersion">The other version.</param>
    /// <param name="otherDefinitionSource">The source of the other version.</param>
    /// <param name="ignoreVersionsBound">True to accept bounds that don't overlap: the greater one wins.</param>
    /// <param name="detailedLogLevel">The log level of an upgrade.</param>
    /// <param name="logWarn">False to not log the warning of bounds that don't overlap when <paramref name="ignoreVersionsBound"/> is true.</param>
    /// <param name="otherWins">True when the other version has been retained.</param>
    /// <returns>True on success, false on error (bounds that don't overlap and <paramref name="ignoreVersionsBound"/> is false).</returns>
    public static bool MergeVersionBound( IActivityMonitor monitor,
                                          string subject,
                                          ref SVersionBound version,
                                          ref string definitionSource,
                                          SVersionBound otherVersion,
                                          string otherDefinitionSource,
                                          bool ignoreVersionsBound,
                                          LogLevel detailedLogLevel,
                                          bool logWarn,
                                          out bool otherWins )
    {
        otherWins = false;
        if( version == otherVersion ) return true;
        if( version.Contains( otherVersion ) )
        {
            monitor.Log( detailedLogLevel, $"{subject}: upgraded from '{version}' (by '{definitionSource}') to '{otherVersion}' (by '{otherDefinitionSource}')." );
            version = otherVersion;
            definitionSource = otherDefinitionSource;
            otherWins = true;
        }
        else if( !otherVersion.Contains( version ) )
        {
            if( !ignoreVersionsBound )
            {
                monitor.Error( $"""
                    {subject}: incompatible versions detected between current '{version}' (by '{definitionSource}') and '{otherVersion}' (by '{otherDefinitionSource}').
                    Set IgnoreVersionsBound to true to allow the upgrade.
                    """ );
                return false;
            }
            otherWins = otherVersion.Base > version.Base
                        || (otherVersion.Base == version.Base && otherVersion.Lock > version.Lock);
            SVersionBound newV = otherWins ? otherVersion : version;
            if( logWarn )
            {
                monitor.Warn( $"""
                    {subject}: current version '{version}' (by '{definitionSource}') is not compatible with '{otherVersion}' (by '{otherDefinitionSource}').
                    Since IgnoreVersionsBound is true, the version is updated to '{newV}'.
                    """ );
            }
            version = newV;
            if( otherWins ) definitionSource = otherDefinitionSource;
        }
        return true;
    }

    internal void Update( IActivityMonitor monitor, LogLevel detailedLogLevel, DependencyKind otherDep, string otherDefinitionSource )
    {
        if( otherDep > _dependencyKind )
        {
            monitor.Log( detailedLogLevel, $"TypeScript library '{_name}': Kind changed from '{_dependencyKind}' to '{otherDep}' (by '{otherDefinitionSource}')." );
            _dependencyKind = otherDep;
        }
    }

}
