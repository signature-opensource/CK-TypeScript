using CK.EmbeddedResources;
using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// Exposes the <see cref="ResPackage.Resources"/> and <see cref="ResPackage.ResourcesAfter"/>.
/// </summary>
public interface IResPackageResources
{
    /// <summary>
    /// Gets whether these resources are the <see cref="ResPackage.ResourcesAfter"/> (or the <see cref="ResPackage.Resources"/>).
    /// </summary>
    bool IsAfter { get; }

    /// <summary>
    /// Gets the index of this package resources in the <see cref="ResourceSpaceData.AllPackageResources"/>.
    /// </summary>
    int Index { get; }

    /// <summary>
    /// Gets the resources.
    /// </summary>
    IResourceContainer Resources { get; }

    /// <summary>
    /// 
    /// TODO: replaces this by a bool IsReachable( IResPackageResource ) and/or bool IsReachable( CodeStoreResources )
    ///       that will handle the closure.
    /// 
    /// Gets the other package resources that are reachable from this one.
    /// <list type="bullet">
    ///     <item>
    ///     When <see cref="IsAfter"/> is false, these are the <see cref="ResPackage.ReachablePackages"/>'s <see cref="ResPackage.ResourcesAfter"/>.
    ///     </item>
    ///     <item>When <see cref="IsAfter"/> is true, these are the <see cref="ResPackage.AfterReachablePackages"/>'s <see cref="ResPackage.ResourcesAfter"/>
    ///     plus this <see cref="Package"/>'s <see cref="ResPackage.Resources"/>.
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

