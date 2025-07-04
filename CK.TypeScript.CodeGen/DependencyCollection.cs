using CK.Core;
using CSemVer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Mutable collection of <see cref="PackageDependency"/>.
/// <para>
/// Npm "latest" version can be managed by using the <see cref="SVersionBound.All"/>
/// and "workspace:" dependencies can be managed with <see cref="SVersionBound.None"/>.
/// See <see cref="PackageDependency.IsLatestDependency"/> and <see cref="PackageDependency.IsWorkspaceDependency"/>.
/// </para>
/// </summary>
public sealed class DependencyCollection : IReadOnlyDictionary<string, PackageDependency>
{
    readonly Dictionary<string, PackageDependency> _dependencies;
    readonly bool _ignoreVersionsBound;

    /// <summary>
    /// Initializes a new empty dependency collection.
    /// </summary>
    /// <param name="ignoreVersionsBound">See <see cref="LibraryManager.IgnoreVersionsBound"/>.</param>
    public DependencyCollection( bool ignoreVersionsBound )
    {
        _dependencies = new Dictionary<string, PackageDependency>();
        _ignoreVersionsBound = ignoreVersionsBound;
    }

    /// <summary>
    /// Copy constructor with optional filtering.
    /// </summary>
    /// <param name="from">The source dependencies.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="cloneDependencies">False to reference the source dependencies.</param>
    public DependencyCollection( DependencyCollection from, Func<PackageDependency, bool>? filter = null, bool cloneDependencies = true )
    {
        _dependencies = new Dictionary<string, PackageDependency>();
        _ignoreVersionsBound = from.IgnoreVersionsBound;
        var source = from.Values;
        if( filter != null ) source = source.Where( filter );
        if( cloneDependencies ) source = source.Select( d => new PackageDependency( d.Name, d.Version, d.DependencyKind, d.DefinitionSource ) );
        foreach( var d in source )
        {
            _dependencies.Add( d.Name, d );
        }
    }

    /// <summary>
    /// Gets whether this collection skips the version bound check when updating a <see cref="PackageDependency"/>.
    /// See <see cref="LibraryManager.IgnoreVersionsBound"/>.
    /// </summary>
    public bool IgnoreVersionsBound => _ignoreVersionsBound;

    /// <summary>
    /// Removes a dependency.
    /// </summary>
    /// <param name="name">The <see cref="PackageDependency.Name"/> to remove.</param>
    /// <returns>True if it has been removed, false if not found.</returns>
    public bool Remove( string name ) => _dependencies.Remove( name );

    /// <summary>
    /// Removes all <see cref="PackageDependency.IsLatestDependency"/> dependencies and returns them.
    /// </summary>
    /// <returns>The dependencies that should be installed explicitly.</returns>
    public IList<PackageDependency> RemoveLatestDependencies()
    {
        var l = _dependencies.Values.Where( d => d.Version == SVersionBound.All ).ToList();
        if( l.Count > 0 )
        {
            foreach( var d in l ) _dependencies.Remove( d.Name );
        }
        return l;
    }

    /// <summary>
    /// Unconditionnally adds or replaces a dependency with a clone by default.
    /// </summary>
    /// <param name="dependency">The dependency.</param>
    /// <param name="cloneAddedDependency">False to add this reference.</param>
    public void AddOrReplace( PackageDependency dependency, bool cloneAddedDependency = true )
    {
        _dependencies[dependency.Name] = cloneAddedDependency
                                            ? new PackageDependency( dependency.Name, dependency.Version, dependency.DependencyKind, dependency.DefinitionSource )
                                            : dependency;
    }

    /// <summary>
    /// Adds or updates a dependency to this collection.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="dependency">The dependency to merge.</param>
    /// <param name="detailedLogLevel">Log level for upgrades of version or kind. Use <see cref="LogLevel.None"/> to silent them.</param>
    /// <param name="cloneAddedDependency">
    /// By default, when the <paramref name="dependency"/> doesn't exist a clone is added in this collection.
    /// Sets this to false to reference the provided instance.
    /// </param>
    /// <returns>True on success, false otherwise.</returns>
    public bool AddOrUpdate( IActivityMonitor monitor,
                             PackageDependency dependency,
                             LogLevel detailedLogLevel = LogLevel.Trace,
                             bool cloneAddedDependency = true )
    {
        if( _dependencies.TryGetValue( dependency.Name, out var our ) )
        {
            if( !our.Update( monitor, dependency, _ignoreVersionsBound, detailedLogLevel ) )
            {
                return false;
            }
        }
        else
        {
            _dependencies.Add( dependency.Name,
                               cloneAddedDependency
                                ? new PackageDependency( dependency.Name, dependency.Version, dependency.DependencyKind, dependency.DefinitionSource )
                                : dependency );
        }
        return true;
    }

    /// <summary>
    /// Merges the <paramref name="dependencies"/> (upgrade existing ones or creates new ones) in this collection).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="dependencies">The dependencies to merge.</param>
    /// <param name="detailedLogLevel">Log level for upgrades of version or kind. Use <see cref="LogLevel.None"/> to silent them.</param>
    /// <param name="cloneDependencies">False to not clone an added dependency.</param>
    /// <returns>True on success, false otherwise.</returns>
    public bool AddOrUpdate( IActivityMonitor monitor,
                             IEnumerable<PackageDependency> dependencies,
                             LogLevel detailedLogLevel = LogLevel.Trace,
                             bool cloneDependencies = true )
    {
        bool success = true;
        foreach( var d in dependencies )
        {
            if( !AddOrUpdate( monitor, d, detailedLogLevel, cloneDependencies ) )
            {
                success = false;
            }
        }
        return success;
    }

    /// <inheritdoc />
    public int Count => _dependencies.Count;

    /// <inheritdoc />
    public IEnumerable<string> Keys => _dependencies.Keys;

    /// <inheritdoc />
    public IEnumerable<PackageDependency> Values => _dependencies.Values;

    /// <inheritdoc />
    public PackageDependency this[string key] => _dependencies[key];

    /// <summary>
    /// Removes all dependencies from this collection.
    /// </summary>
    public void Clear() => _dependencies.Clear();

    /// <inheritdoc />
    public bool ContainsKey( string key ) => _dependencies.ContainsKey( key );

    /// <inheritdoc />
    public bool TryGetValue( string key, [MaybeNullWhen( false )] out PackageDependency value ) => _dependencies.TryGetValue( key, out value );

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, PackageDependency>> GetEnumerator() => _dependencies.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _dependencies.GetEnumerator();

}
