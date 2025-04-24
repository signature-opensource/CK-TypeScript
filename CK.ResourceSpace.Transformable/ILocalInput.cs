using static CK.Core.ActivityMonitorErrorCounter;

namespace CK.Core;

/// <summary>
/// Local imput is a <see cref="LocalFunctionSource"/> or a <see cref="LocalItem"/>.
/// </summary>
interface ILocalInput : IResourceInput
{
    /// <summary>
    /// Gets the full local path.
    /// </summary>
    string FullPath { get; }

    /// <summary>
    /// Gets or sets the previous input in this <see cref="IResourceInput.Resources"/>.
    /// </summary>
    ILocalInput? Prev { get; set; }

    /// <summary>
    /// Gets or sets the next input in this <see cref="IResourceInput.Resources"/>.
    /// </summary>
    ILocalInput? Next { get; set; }

    void OnChange( IActivityMonitor monitor, TransformEnvironment environment );

    void ApplyChanges( IActivityMonitor monitor, TransformEnvironment environment );
}
