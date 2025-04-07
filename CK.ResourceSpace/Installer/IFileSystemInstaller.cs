using System.IO;

namespace CK.Core;

/// <summary>
/// Specialized installer: items are written to the file system, in
/// the <see cref="TargetPath"/> folder.
/// </summary>
public interface IFileSystemInstaller : IResourceSpaceItemInstaller
{
    /// <summary>
    /// Gets the target path. This must end with the <see cref="Path.DirectorySeparatorChar"/>.
    /// </summary>
    string TargetPath { get; }
}
