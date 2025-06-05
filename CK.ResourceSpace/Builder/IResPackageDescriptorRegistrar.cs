using CK.EmbeddedResources;
using System;

namespace CK.Core;

/// <summary>
/// This is the only way to instantiate <see cref="ResPackageDescriptor"/>.
/// </summary>
public interface IResPackageDescriptorRegistrar
{
    /// <summary>
    /// Registers a package. It must not already exist: <paramref name="fullName"/>, <paramref name="resourceStore"/>
    /// or <paramref name="resourceAfterStore"/> must not have been already registered.
    /// <para>
    /// <paramref name="resourceStore"/> and <paramref name="resourceAfterStore"/> must be <see cref="IResourceContainer.IsValid"/>
    /// and must not be the same instance.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="fullName">The package full name.</param>
    /// <param name="defaultTargetPath">The default target path associated to the package's resources.</param>
    /// <param name="resourceStore">
    /// The package resources. Must be <see cref="IResourceContainer.IsValid"/>.
    /// </param>
    /// <param name="resourceAfterStore">
    /// The package resources to apply after the <see cref="ResPackageDescriptor.Children"/>.
    /// Must be <see cref="IResourceContainer.IsValid"/>.
    /// </param>
    /// <param name="isOptional">
    /// Defines the initial value of <see cref="ResPackageDescriptor.IsOptional"/>. By default, this
    /// is specified by the CKPackage manifest and if there is no CKPackage manifest, this is false.
    /// </param>
    /// <returns>The package descriptor on success, null on error.</returns>
    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                  string fullName,
                                                  NormalizedPath defaultTargetPath,
                                                  IResourceContainer resourceStore,
                                                  IResourceContainer resourceAfterStore,
                                                  bool? isOptional );

    /// <summary>
    /// Registers a package. It must not already exist: <paramref name="type"/> must not have been already registered.
    /// <para>
    /// The <see cref="ResPackageDescriptor.DefaultTargetPath"/> is derived from the type's namespace:
    /// "The/Type/Namespace" (the dots of the namespace are replaced with a '/'.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="type">The type that defines the package.</param>
    /// <param name="isOptional">
    /// Defines the initial value of <see cref="ResPackageDescriptor.IsOptional"/>. By default, this
    /// is driven by the existence of the <see cref="OptionalTypeAttribute">[OptionalType]</see> attribute.
    /// </param>
    /// <param name="ignoreLocal">
    /// True to ignore local folder: even if the resources are in a local folder, this creates
    /// a <see cref="AssemblyResourceContainer"/> instead of a <see cref="FileSystemResourceContainer"/>
    /// for both <see cref="ResPackageDescriptor.Resources"/> and <see cref="ResPackageDescriptor.AfterResources"/>.
    /// <para>
    /// This is mainly for tests.
    /// </para>
    /// </param>
    /// <returns>The package descriptor on success, null on error.</returns>
    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                  Type type,
                                                  bool? isOptional = null,
                                                  bool ignoreLocal = false );

    /// <summary>
    /// Registers a package. It must not already exist: <paramref name="type"/> must not have been already registered.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="type">The type that defines the package.</param>
    /// <param name="defaultTargetPath">The default target path associated to the package's resources.</param>
    /// <param name="isOptional">
    /// Defines the initial value of <see cref="ResPackageDescriptor.IsOptional"/>. By default, this
    /// is driven by the existence of the <see cref="OptionalTypeAttribute">[OptionalType]</see> attribute.
    /// </param>
    /// <param name="ignoreLocal">
    /// True to ignore local folder: even if the resources are in a local folder, this creates
    /// a <see cref="AssemblyResourceContainer"/> instead of a <see cref="FileSystemResourceContainer"/>
    /// for both <see cref="ResPackageDescriptor.Resources"/> and <see cref="ResPackageDescriptor.AfterResources"/>.
    /// <para>
    /// This is mainly for tests.
    /// </para>
    /// </param>
    /// <returns>The package descriptor on success, null on error.</returns>
    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                  Type type,
                                                  NormalizedPath defaultTargetPath,
                                                  bool? isOptional = null, 
                                                  bool ignoreLocal = false );

}
