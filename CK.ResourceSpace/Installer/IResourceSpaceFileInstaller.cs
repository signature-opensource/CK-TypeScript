using CK.EmbeddedResources;
using System.IO;

namespace CK.Core;

/// <summary>
/// Resources final target.
/// </summary>
public interface IResourceSpaceFileInstaller
{
    /// <summary>
    /// Writes a resource content to the file system.
    /// Its target file path is <see cref="ResourceLocator.ResourceName"/>.
    /// </summary>
    /// <param name="resource">The resource to write.</param>
    void Write( ResourceLocator resource );

    /// <summary>
    /// Opens a writable stream on the file system.
    /// </summary>
    /// <param name="filePath">The physical file path.</param>
    /// <returns>The stream that must be disposed once written.</returns>
    Stream OpenWriteStream( NormalizedPath filePath );

    /// <summary>
    /// Writes a resource content to the file system.
    /// </summary>
    /// <param name="filePath">The physical file path.</param>
    /// <param name="resource">The resource to write.</param>
    void Write( NormalizedPath filePath, ResourceLocator resource );

    /// <summary>
    /// Writes a text content to the file system.
    /// </summary>
    /// <param name="filePath">The physical file path.</param>
    /// <param name="text">The text content to write.</param>
    void Write( NormalizedPath filePath, string text );

}
