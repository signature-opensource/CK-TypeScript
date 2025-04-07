using CK.Core;
using CSemVer;
using System.Collections.Generic;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Mutable collection of <see cref="PackageDependency"/>.
/// See <see cref="TypeScriptFileCollector.GeneratedDependencies"/>.
/// <para>
/// Npm "latest" version can be managed by using the <see cref="SVersionBound.All"/>
/// and "workspace:" dependencies can be managed with <see cref="SVersionBound.None"/>.
/// See <see cref="PackageDependency.IsLatestDependency"/> and <see cref="PackageDependency.IsWorkspaceDependency"/>.
/// </para>
/// </summary>
public interface IDependencyCollection : IReadOnlyDictionary<string, PackageDependency>
{
    /// <summary>
    /// Gets whether this collection skips the version bound check when updating a <see cref="PackageDependency"/>.
    /// See <see cref="LibraryManager.IgnoreVersionsBound"/>.
    /// </summary>
    bool IgnoreVersionsBound { get; }

    /// <summary>
    /// Unconditionnally adds or replaces a dependency with a clone by default.
    /// </summary>
    /// <param name="dependency">The dependency.</param>
    /// <param name="cloneAddedDependency">False to add this reference.</param>
    void AddOrReplace( PackageDependency dependency, bool cloneAddedDependency = true );

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
    bool AddOrUpdate( IActivityMonitor monitor, PackageDependency dependency, LogLevel detailedLogLevel = LogLevel.Trace, bool cloneAddedDependency = true );

    /// <summary>
    /// Merges the <paramref name="dependencies"/> (upgrade existing ones or creates new ones) in this collection).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="dependencies">The dependencies to merge.</param>
    /// <param name="detailedLogLevel">Log level for upgrades of version or kind. Use <see cref="LogLevel.None"/> to silent them.</param>
    /// <param name="cloneDependencies">False to not clone an added dependency.</param>
    /// <returns>True on success, false otherwise.</returns>
    bool AddOrUpdate( IActivityMonitor monitor, IEnumerable<PackageDependency> dependencies, LogLevel detailedLogLevel = LogLevel.Trace, bool cloneDependencies = true );

    /// <summary>
    /// Removes all dependencies from this collection.
    /// </summary>
    void Clear();

    /// <summary>
    /// Removes a dependency.
    /// </summary>
    /// <param name="name">The <see cref="PackageDependency.Name"/> to remove.</param>
    /// <returns>True if it has been removed, false if not found.</returns>
    bool Remove( string name );

    /// <summary>
    /// Removes all <see cref="PackageDependency.IsLatestDependency"/> dependencies and returns them.
    /// </summary>
    /// <returns>The dependencies that should be installed explicitly.</returns>
    IList<PackageDependency> RemoveLatestDependencies();
}
