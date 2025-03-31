namespace CK.Core;

/// <summary>
/// Minimal Live interface that must handle defferred application of any number of changes.
/// </summary>
public interface ILiveUpdater
{
    /// <summary>
    /// Called when a path has changed. The file may have been removed.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="filePath">The full file path.</param>
    void OnChange( IActivityMonitor monitor, string filePath );

    /// <summary>
    /// Must apply all changes collected so far.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>True on success, false otherwise. Errors must be logged.</returns>
    bool ApplyChanges( IActivityMonitor monitor );
}
