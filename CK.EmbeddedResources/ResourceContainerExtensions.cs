using CK.Core;
using System;

namespace CK.EmbeddedResources;

/// <summary>
/// Extends <see cref="IResourceContainer"/>.
/// </summary>
public static class ResourceContainerExtensions
{
    /// <summary>
    /// Gets the local file path of the root of this container.
    /// </summary>
    /// <param name="container">This container.</param>
    /// <returns>The container's local path or null.</returns>
    public static string? GetLocalPath( this IResourceContainer container )
    {
        if( container.HasLocalFilePathSupport )
        {
            var root = new ResourceLocator( container, container.ResourcePrefix );
            return container.GetLocalFilePath( root );
        }
        return null;
    }

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

    /// <summary>
    /// Tries to get an existing resource.
    /// </summary>
    /// <param name="container">This container.</param>
    /// <param name="localResourceName">The local resource name (can contain any folder prefix).</param>
    /// <param name="locator">The resulting locator.</param>
    /// <returns>True if the resource exists, false otherwise.</returns>
    public static bool TryGetResource( this IResourceContainer container, ReadOnlySpan<char> localResourceName, out ResourceLocator locator )
    {
        locator = container.GetResource( localResourceName );
        return locator.IsValid;
    }

    /// <summary>
    /// Tries to get an existing folder.
    /// </summary>
    /// <param name="container">This container.</param>
    /// <param name="localFolderName">The local resource folder name (can contain any folder prefix).</param>
    /// <param name="folder">The resulting folder.</param>
    /// <returns>True if the folder exists, false otherwise.</returns>
    public static bool TryGetFolder( this IResourceContainer container,  ReadOnlySpan<char> localFolderName, out ResourceFolder folder )
    {
        folder = container.GetFolder( localFolderName );
        return folder.IsValid;
    }

    /// <summary>
    /// Tries to get an existing resource in this folder.
    /// </summary>
    /// <param name="parentFolder">This folder.</param>
    /// <param name="localResourceName">The local resource name (can contain any folder prefix).</param>
    /// <param name="locator">The resulting locator.</param>
    /// <returns>True if the resource exists, false otherwise.</returns>
    public static bool TryGetResource( this ResourceFolder parentFolder, ReadOnlySpan<char> localResourceName, out ResourceLocator locator )
    {
        locator = parentFolder.GetResource( localResourceName );
        return locator.IsValid;
    }

    /// <summary>
    /// Tries to get an existing folder in this folder.
    /// </summary>
    /// <param name="folder">This folder.</param>
    /// <param name="localFolderName">The local resource folder name (can contain any folder prefix).</param>
    /// <param name="folder">The resulting folder.</param>
    /// <returns>True if the folder exists, false otherwise.</returns>
    public static bool TryGetFolder( this ResourceFolder parentFolder,  ReadOnlySpan<char> localFolderName, out ResourceFolder folder )
    {
        folder = parentFolder.GetFolder( localFolderName );
        return folder.IsValid;
    }

}
