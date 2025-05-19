namespace CK.Core;

/// <summary>
/// Extends <see cref="IResourceSpaceItemInstaller"/> to handle suppression.
/// </summary>
public interface ILiveResourceSpaceItemInstaller : IResourceSpaceItemInstaller
{
    /// <summary>
    /// Deletes the item.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="path">The item path to delete.</param>
    void SafeDelete( IActivityMonitor monitor, NormalizedPath path );
}
