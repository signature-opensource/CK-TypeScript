using System.Collections.Immutable;

namespace CK.Core;

/// <summary>
/// Basic low-level interface.
/// Instances must be serializable (typically by implementing <see cref="ICKSimpleBinarySerializable"/> support).
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
    /// <param name="finalText">Final text, potentially transformed.</param>
    /// <param name="installer">Final installer to use.</param>
    /// <returns>
    /// True to cancel any further installation for the item.
    /// False to let the next hooks (and utimately the <paramref name="installer"/>) do their job.
    /// </returns>
    bool HandleInstall( IActivityMonitor monitor,
                        ITransformInstallableItem item,
                        string finalText,
                        IResourceSpaceItemInstaller installer );

    /// <summary>
    /// Called after all <see cref="HandleInstall"/> calls.
    /// </summary>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="installer">Final installer to use.</param>
    void StopInstall( IActivityMonitor monitor, IResourceSpaceItemInstaller installer );
}
