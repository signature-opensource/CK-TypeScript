using System;
using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// Strategy used to resolve <see cref="ResPackageDescriptor"/> from types.
/// </summary>
public interface IResPackageDescriptorResolver
{
    /// <summary>
    /// Must find a package descriptor from a <paramref name="targetType"/>.
    /// This is used only for <see cref="PackageAttribute{T}"/> but may be useful for other
    /// kind of 0-to-1 relationships.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="packageIndex">The <see cref="ResSpaceCollector.PackageIndex"/>.</param>
    /// <param name="relName">The relationship name ("Package").</param>
    /// <param name="targetType">The target type to resolve.</param>
    /// <param name="decoratedType">The type that declares the relationships.</param>
    /// <param name="result">The resolved package if any.</param>
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    bool TryFindSinglePackageDescriptorByType( IActivityMonitor monitor,
                                               IReadOnlyDictionary<object, ResPackageDescriptor> packageIndex,
                                               string relName,
                                               Type targetType,
                                               Type decoratedType,
                                               out ResPackageDescriptor? result );
    /// <summary>
    /// Must find any number of package. Used for multiple relationships: <paramref name="relName"/> can
    /// be "Requires", "RequiredBy", "Children", "Groups".
    /// One <paramref name="targetType"/> can resolve to multiple packages: the <paramref name="collector"/>
    /// collects them.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="packageIndex">The <see cref="ResSpaceCollector.PackageIndex"/>.</param>
    /// <param name="relName">The relationship name.</param>
    /// <param name="targetType">The target type to resolve.</param>
    /// <param name="decoratedType">The type that declares the relationships.</param>
    /// <param name="collector">The result collector.</param>
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    bool TryFindMultiplePackageDescriptorByType( IActivityMonitor monitor,
                                                 IReadOnlyDictionary<object, ResPackageDescriptor> packageIndex,
                                                 string relName,
                                                 Type targetType,
                                                 Type decoratedType,
                                                 Action<ResPackageDescriptor> collector );

}

