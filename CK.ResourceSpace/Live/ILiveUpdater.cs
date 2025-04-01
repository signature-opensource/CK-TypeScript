namespace CK.Core;

/// <summary>
/// Minimal Live interface that must handle defferred application of any number of changes.
/// </summary>
public interface ILiveUpdater
{
    /// <summary>
    /// Called when a path has changed. The file may have been removed. When <paramref name="filePath"/>
    /// is empty, it means that a "global" change at the <see cref="IResPackageResources.LocalPath"/> folder
    /// level occurred.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="resources">
    /// The package's resources that changed.
    /// <see cref="IResPackageResources.LocalPath"/> is necessarily not null.
    /// </param>
    /// <param name="filePath">The path in the <paramref name="resources"/>.</param>
    /// <returns>Must return true if the change is handled by this handler, false if the change doesn't concern this handler.</returns>
    bool OnChange( IActivityMonitor monitor, IResPackageResources resources, string filePath );

    /// <summary>
    /// Must apply all changes collected so far.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="installer">The target installer.</param>
    /// <returns>True on success, false otherwise. Errors must be logged.</returns>
    bool ApplyChanges( IActivityMonitor monitor, IResourceSpaceFileInstaller installer );
}
