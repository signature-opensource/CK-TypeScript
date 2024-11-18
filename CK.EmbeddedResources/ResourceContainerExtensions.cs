namespace CK.Core;

/// <summary>
/// Extends <see cref="IResourceContainer"/>.
/// </summary>
public static class ResourceContainerExtensions
{
    /// <summary>
    /// Tries to get an existing resource and logs an error if it is not found.
    /// </summary>
    /// <param name="container">This container.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="resourcePath">The resource path.</param>
    /// <param name="locator">The resulting locator.</param>
    /// <returns>True if the resource exists, false otherwise.</returns>
    public static bool TryGetResource( this IResourceContainer container, IActivityMonitor monitor, string resourcePath, out ResourceLocator locator )
    {
        if( !container.TryGetResource( resourcePath, out locator ) )
        {
            monitor.Error( $"Unable to find expected resource '{resourcePath}' from {container.DisplayName}." );
            return false;
        }
        return true;
    }
}
