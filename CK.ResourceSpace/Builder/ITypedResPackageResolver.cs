using System;

namespace CK.Core;

/// <summary>
/// Handles automatic registration of a <see cref="ResPackageDescriptor"/> from its type.
/// </summary>
public interface ITypedResPackageResolver
{
    /// <summary>
    /// Must register a type-bound package from its type or emits an error.
    /// <para>
    /// The <paramref name="type"/> is necessarily a <see cref = "IResourceGroup" /> that has
    /// been allowed by the <see cref="IResPackageExcluder"/>.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor that must receive detailed errors if the package cannot be registered.</param>
    /// <param name="registrar">The package registrar.</param>
    /// <param name="type">The candidate type.</param>
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    bool ResolveRequired( IActivityMonitor monitor, IResPackageDescriptorRegistrar registrar, Type type );
}
