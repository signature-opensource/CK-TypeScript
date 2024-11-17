using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CK.Core;

/// <summary>
/// Abstract resource container. Its goal is to handle <see cref="ResourceLocator"/> and
/// to support <see cref="IFileProvider"/> abstraction.
/// <para>
/// An <see cref="AssemblyResources"/> is not a <see cref="IResourceContainer"/> (because we cannot know the
/// path separator to use for all the resources) but it can create subordinate containers on the "ck@" prefixed resources
/// (that uses '/' as the path separator) thanks to <see cref="AssemblyResources.CreateResourcesContainerForType(CK.Core.IActivityMonitor, Type, string?)"/>.
/// </para>
/// <para>
/// The <see cref="FileSystemResourceContainer"/> is a simple container for file system directories.
/// </para>
/// </summary>
public interface IResourceContainer
{
    /// <summary>
    /// Gets whether this <see cref="IResourceContainer"/> is valid.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Gets a name that identifies this container.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the common prefix of all resources managed by this container.
    /// This can be empty for some kind of containers.
    /// </summary>
    string ResourcePrefix { get; }

    /// <summary>
    /// Gets a resource content.
    /// <para>
    /// If a stream cannot be obtained, a detailed <see cref="IOException"/> is raised.
    /// </para>
    /// </summary>
    /// <param name="resourceName">The resource name.</param>
    /// <returns>The resource's content stream.</returns>
    Stream GetStream( ResourceLocator resource );

    /// <summary>
    /// Tries to get an existing resource.
    /// </summary>
    /// <param name="localResourceName">The local resource name.</param>
    /// <param name="locator">The resulting locator.</param>
    /// <returns>True if the resource exists, false otherwise.</returns>
    bool TryGetResource( ReadOnlySpan<char> localResourceName, out ResourceLocator locator );

    /// <summary>
    /// Gets a <see cref="ResourceLocator"/> from a <see cref="IFileInfo"/> that must have been created
    /// from this <see cref="GetFileProvider()"/> otherwise the returned <see cref="ResourceLocator.IsValid"/> is false.
    /// </summary>
    /// <param name="fileInfo">The file info.</param>
    /// <returns>The resource locator (<see cref="ResourceLocator.IsValid"/> may be false).</returns>
    ResourceLocator GetResourceLocator( IFileInfo fileInfo );

    /// <summary>
    /// Gets all the existing <see cref="ResourceLocator"/>.
    /// </summary>
    IEnumerable<ResourceLocator> AllResources { get; }

    /// <summary>
    /// Creates a <see cref="IFileProvider"/> on the resources.
    /// </summary>
    /// <returns>A file provider.</returns>
    IFileProvider GetFileProvider();

    /// <summary>
    /// Checks whether a directory exists without necessarily using the <see cref="GetFileProvider()"/>.
    /// <para>
    /// For some containers, this can be a welcome optimization.
    /// </para>
    /// </summary>
    /// <param name="localResourceName">The local directory name to lookup.</param>
    /// <returns>True if this container has the directory.</returns>
    bool HasDirectory( ReadOnlySpan<char> localResourceName );

    /// <summary>
    /// Gets the string comparer to use for the resource names.
    /// </summary>
    StringComparer ResourceNameComparer { get; }

    /// <summary>
    /// Returns the <see cref="DisplayName"/>.
    /// </summary>
    /// <returns>This container's DisplayName.</returns>
    string ToString();
}
