using System.Collections.Immutable;

namespace CK.Core;

/// <summary>
/// Basic low-level interface used by initial <see cref="TransformableFileHandler"/> and by
/// Live hooks.
/// </summary>
public interface ITransformableFileInstallHook
{
    /// <summary>
    /// Called before any <see cref="HandleInstall"/> call.
    /// </summary>
    /// <param name="monitor">Monitor to use.</param>
    void StartInstall( IActivityMonitor monitor );

    /// <summary>
    /// Handles the item to install. Returning true prevents any subsequent hooks (and utimately
    /// the <paramref name="installer"/>) to be called.
    /// </summary>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="item">The item to install.</param>
    /// <param name="finalText">Final text, potentially transformed and that can be modified by this hook.</param>
    /// <param name="installer">Final installer to use.</param>
    /// <param name="handled">
    /// True to cancel any further installation for the item.
    /// False to let the next hooks (and utimately the <paramref name="installer"/>) do their job.
    /// </param>
    /// <returns>
    /// True on success, false on error. Errors must be logged.
    /// </returns>
    bool HandleInstall( IActivityMonitor monitor,
                        ITransformInstallableItem item,
                        string finalText,
                        IResourceSpaceItemInstaller installer,
                        out bool handled );

    /// <summary>
    /// Called after all <see cref="HandleInstall"/> calls.
    /// </summary>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="success">Whether the install succeeded.</param>
    /// <param name="installer">Final installer to use.</param>
    void StopInstall( IActivityMonitor monitor, bool success, IResourceSpaceItemInstaller installer );
}



