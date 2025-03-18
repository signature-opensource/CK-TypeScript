using CK.EmbeddedResources;
using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// Exposes the <see cref="ResPackage.BeforeResources"/> and <see cref="ResPackage.AfterResources"/>.
/// </summary>
public interface IResPackageResources
{
    /// <summary>
    /// Gets whether these resources are the <see cref="ResPackage.AfterResources"/> (or the <see cref="ResPackage.BeforeResources"/>).
    /// </summary>
    bool IsAfter { get; }

    /// <summary>
    /// Gets the index of this package resources in the <see cref="ResourceSpaceData.AllPackageResources"/>.
    /// </summary>
    int Index { get; }

    /// <summary>
    /// Gets the resources.
    /// </summary>
    CodeStoreResources Resources { get; }

    /// <summary>
    /// Gets the other package resources that are reachable from this one.
    /// <list type="bullet">
    ///     <item>
    ///     When <see cref="IsAfter"/> is false, these are the <see cref="ResPackage.ReachablePackages"/>'s <see cref="ResPackage.AfterResources"/>.
    ///     </item>
    ///     <item>When <see cref="IsAfter"/> is true, these are the <see cref="ResPackage.AfterReachablePackages"/>'s <see cref="ResPackage.AfterResources"/>
    ///     plus this <see cref="Package"/>'s <see cref="ResPackage.BeforeResources"/>.
    ///     </item>
    /// </list>
    /// This set is minimal (no duplicates nor transitive dependencies).
    /// </summary>
    IEnumerable<IResPackageResources> Reachables { get; }

    /// <summary>
    /// Gets the package that defines these resources.
    /// </summary>
    ResPackage Package { get; }
}

