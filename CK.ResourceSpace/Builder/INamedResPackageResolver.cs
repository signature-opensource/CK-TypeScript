namespace CK.Core;

/// <summary>
/// Handles automatic registration of a <see cref="ResPackageDescriptor"/> from its full name.
/// </summary>
public interface INamedResPackageResolver
{
    /// <summary>
    /// Must register a named package from its full name or emits an error.
    /// <para>
    /// The <paramref name="fullName"/> has been allowed by the <see cref="IResPackageExcluder"/>.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor that must receive detailed errors if the package cannot be registered.</param>
    /// <param name="registrar">The package registrar.</param>
    /// <param name="fullName">The package full name.</param>
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    bool ResolveRequired( IActivityMonitor monitor, IResPackageDescriptorRegistrar registrar, string fullName );
}
