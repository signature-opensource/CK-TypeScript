namespace CK.Core;

/// <summary>
/// Extends the initial installer to also be able to handle suppression of items.
/// </summary>
public interface ILiveTransformableFileInstallHook : ITransformableFileInstallHook
{
    /// <summary>
    /// Handles the suppression of an item. In Live hooks, after the call to
    /// <see cref="ITransformableFileInstallHook.StartInstall"/>, all suppressed
    /// items are submitted to this method and then all new or modified items are submitted to
    /// the <see cref="ITransformableFileInstallHook.HandleInstall"/> method.
    /// </summary>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="item">The item to install.</param>
    /// <param name="installer">
    /// Live installer to use (exposes
    /// the <see cref="ILiveResourceSpaceItemInstaller.SafeDelete(IActivityMonitor, NormalizedPath)"/> method.
    /// </param>
    /// <param name="handled">
    /// True to cancel any further operation for the item.
    /// False to let the next hooks (and utimately the <paramref name="installer"/>) do their job.
    /// </param>
    /// <returns>
    /// True on success, false on error. Errors must be logged.
    /// </returns>
    bool HandleRemove( IActivityMonitor monitor,
                       ITransformInstallableItem item,
                       ILiveResourceSpaceItemInstaller installer,
                       out bool handled );
}



