using CK.EmbeddedResources;

namespace CK.Core;

/// <summary>
/// Models the <see cref="ResPackage.Resources"/> and <see cref="ResPackage.AfterResources"/>.
/// </summary>
public interface IResPackageResources
{
    /// <summary>
    /// Gets whether these resources are the <see cref="ResPackage.AfterResources"/> (or the <see cref="ResPackage.Resources"/>).
    /// </summary>
    bool IsAfter { get; }

    /// <summary>
    /// Gets the index of this package resources in the <see cref="ResSpaceData.AllPackageResources"/>.
    /// </summary>
    int Index { get; }


    /// <summary>
    /// Gets whether this is the "&lt;Code&gt;" resource (<see cref="IsAfter"/> is true).
    /// </summary>
    bool IsCodeResources { get; }

    /// <summary>
    /// Gets whether this is the "&lt;App&gt;" resource (<see cref="IsAfter"/> is false).
    /// </summary>
    bool IsAppResources { get; }

    /// <summary>
    /// Gets the resources.
    /// </summary>
    IResourceContainer Resources { get; }

    /// <summary>
    /// Gets the package that defines these resources.
    /// </summary>
    ResPackage Package { get; }

    /// <summary>
    /// Gets the local folder path if the <see cref="Resources"/> are in a <see cref="FileSystemResourceContainer"/>
    /// with a true <see cref="FileSystemResourceContainer.HasLocalFilePathSupport"/>.
    /// </summary>
    string? LocalPath { get; }
}

