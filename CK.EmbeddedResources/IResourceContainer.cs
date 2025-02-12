using System;
using System.Collections.Generic;
using System.IO;

namespace CK.Core;

/// <summary>
/// Abstract resource container. Its goal is to handle <see cref="ResourceFolder"/> and <see cref="ResourceLocator"/>.
/// <para>
/// An <see cref="AssemblyResources"/> is not a <see cref="IResourceContainer"/> (because we cannot know the
/// folder separator to use for all the resources) but it can create subordinate containers on the "ck@" prefixed resources
/// (that uses '/' as the folder separator) thanks to <see cref="AssemblyResources.CreateResourcesContainerForType(IActivityMonitor, Type, string?)"/>.
/// </para>
/// <para>
/// The <see cref="FileSystemResourceContainer"/> is a simple container for file system directories and files.
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
    /// Gets a resource content from a locator (that must belong to this container).
    /// <para>
    /// </para>
    /// </summary>
    /// <param name="resource">The resource locator.</param>
    /// <returns>The resource's content stream.</returns>
    Stream GetStream( in ResourceLocator resource );

    /// <summary>
    /// Writes the the resource (that must belong to this container) to a stream.
    /// </summary>
    /// <param name="resource">The resource to write.</param>
    /// <param name="target">The target stream.</param>
    void WriteStream( in ResourceLocator resource, Stream target );

    /// <summary>
    /// Gets an existing resource or a locator with <see cref="ResourceLocator.IsValid"/> false
    /// if the resource doesn't exist.
    /// </summary>
    /// <param name="localResourceName">The local resource name (can contain any folder prefix).</param>
    /// <returns>The resource locator that may not be valid.</returns>
    ResourceLocator GetResource( ReadOnlySpan<char> localResourceName );

    /// <summary>
    /// Gets an existing resource in a <paramref name="folder"/> or a locator with <see cref="ResourceLocator.IsValid"/> false
    /// if the resource doesn't exist.
    /// </summary>
    /// <param name="folder">The parent folder.</param>
    /// <param name="localResourceName">The local resource name (can contain any folder prefix).</param>
    /// <returns>The resource locator that may not be valid.</returns>
    ResourceLocator GetResource( ResourceFolder folder, ReadOnlySpan<char> localResourceName );

    /// <summary>
    /// Gets an existing folder or a ResourceFolder with <see cref="ResourceLocator.IsValid"/> false
    /// if the folder doesn't exist.
    /// </summary>
    /// <param name="localFolderName">The local resource folder name (can contain any folder prefix).</param>
    /// <returns>The resource folder that may not be valid.</returns>
    ResourceFolder GetFolder( ReadOnlySpan<char> localFolderName );

    /// <summary>
    /// Gets an existing folder in a <paramref name="folder"/> or a ResourceFolder with <see cref="ResourceLocator.IsValid"/> false
    /// if the folder doesn't exist.
    /// </summary>
    /// <param name="folder">The parent folder.</param>
    /// <param name="localFolderName">The local resource folder name (can contain any folder prefix).</param>
    /// <returns>The resource folder that may not be valid.</returns>
    ResourceFolder GetFolder( ResourceFolder folder, ReadOnlySpan<char> localFolderName );

    /// <summary>
    /// Gets all the existing <see cref="ResourceLocator"/> regardless of any folder.
    /// To be used when you don't care about folders and want a direct access to the resources.
    /// </summary>
    IEnumerable<ResourceLocator> AllResources { get; }

    /// <summary>
    /// Gets the all the resources contained in <paramref name="folder"/> (that must belong to this container),
    /// regardless of any subordinated folders.
    /// To be used when you don't care about folders and want a direct access to the resources.
    /// </summary>
    /// <param name="folder">The resource folder for which all resources must be enumerated.</param>
    /// <returns>All the resources of the folder.</returns>
    IEnumerable<ResourceLocator> GetAllResources( ResourceFolder folder );

    /// <summary>
    /// Gets the resources contained in a folder (that must belong to this container).
    /// </summary>
    /// <param name="folder">The parent resource folder.</param>
    /// <returns>The resources that this folder contains.</returns>
    IEnumerable<ResourceLocator> GetResources( ResourceFolder folder );

    /// <summary>
    /// Gets the direct children folders contained in <paramref name="folder"/> (that must belong to this container).
    /// </summary>
    /// <param name="folder">The parent resource folder.</param>
    /// <returns>The direct children folders.</returns>
    IEnumerable<ResourceFolder> GetFolders( ResourceFolder folder );

    /// <summary>
    /// Gets the string comparer to use for the resource names.
    /// </summary>
    StringComparer NameComparer { get; }

    /// <summary>
    /// Gets the name of a folder (that must belong to this container)
    /// without any parent folder related information. May be empty for some
    /// resources of some container implementations.
    /// </summary>
    /// <param name="folder">A folder.</param>
    /// <returns>The name of the folder.</returns>
    ReadOnlySpan<char> GetFolderName( ResourceFolder folder );

    /// <summary>
    /// Gets the name of a resource (that must belong to this container)
    /// without any folder related information. May be empty for some
    /// resources of some container implementations.
    /// </summary>
    /// <param name="folder">A folder.</param>
    /// <returns>The name of the resource.</returns>
    ReadOnlySpan<char> GetResourceName( ResourceLocator resource );

    /// <summary>
    /// Gets whether this container can contain <see cref="ResourceLocator"/> that are
    /// bound to a local file on the file system.
    /// <para>
    /// When this is false, <see cref="GetLocalFilePath(in ResourceLocator)"/> always
    /// returns a null path, but true doesn't necessarily mean that all existing resources
    /// have a local file path: only some of them may have a local file path.
    /// </para>
    /// <para>
    /// When a resource has a non null <see cref="ResourceLocator.LocalFilePath"/>, the file
    /// content may be the exact same content as the one returned by <see cref="GetStream(in ResourceLocator)"/>
    /// or may diverge from it either because it has been altered after its content has been captured by
    /// the container or because the resource stream is a projection (a transformation) of the file content.
    /// </para>
    /// </summary>
    bool HasLocalFilePathSupport { get; }

    /// <summary>
    /// Tries to get a local file path for the resource (that must belong to this container).
    /// <para>
    /// This path always uses the platform specific separator <see cref="Path.DirectorySeparatorChar"/>.
    /// </para>
    /// See <see cref="HasLocalFilePathSupport"/>.
    /// </summary>
    /// <param name="resource">The resource locator.</param>
    /// <returns>The resource's local file name or null.</returns>
    string? GetLocalFilePath( in ResourceLocator resource );

    /// <summary>
    /// Returns the <see cref="DisplayName"/>.
    /// </summary>
    /// <returns>This container's DisplayName.</returns>
    string ToString();
}
