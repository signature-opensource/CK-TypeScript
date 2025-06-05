using System;

namespace CK.Core;

/// <summary>
/// Handles package filtering by type or full name.
/// </summary>
public interface IResPackageExcluder
{
    /// <summary>
    /// Returns whether a required type-bound package can be registered
    /// in the <see cref="IResPackageDescriptorRegistrar"/>.
    /// <para>
    /// The <paramref name="type"/> is necessarily a <see cref = "IResourceGroup" />.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor that must receive detailed errors if the type is rejected.</param>
    /// <param name="type">The candidate type.</param>
    /// <returns>Whether the type is allowed or not.</returns>
    bool AllowRequired( IActivityMonitor monitor, Type type );

    /// <summary>
    /// Returns whether a required named package can be registered
    /// in the <see cref="IResPackageDescriptorRegistrar"/>.
    /// </summary>
    /// <param name="monitor">The monitor that must receive detailed errors if the full name is rejected.</param>
    /// <param name="fullName">The candidate package's full name.</param>
    /// <returns>Whether the full name is allowed or not.</returns>
    bool AllowRequired( IActivityMonitor monitor, string fullName );

    /// <summary>
    /// Returns whether an optional type-bound package can be added
    /// to the final <see cref="ResSpace"/>.
    /// This must emits warnings to the <paramref name="monitor"/> if the type is filtered out.
    /// <para>
    /// The <paramref name="type"/> is necessarily a <see cref = "IResourceGroup" />.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor that must receive detailed warnings if the type is rejected.</param>
    /// <param name="type">The candidate type.</param>
    /// <returns>Whether the type is allowed or not.</returns>
    bool AllowOptional( IActivityMonitor monitor, Type type );

    /// <summary>
    /// Returns whether an optional named package can be added
    /// to the final <see cref="ResSpace"/>.
    /// This must emits warnings to the <paramref name="monitor"/> if the type is filtered out.
    /// </summary>
    /// <param name="monitor">The monitor that must receive detailed warnings if the full name is rejected.</param>
    /// <param name="fullName">The candidate package's full name.</param>
    /// <returns>Whether the full name is allowed or not.</returns>
    bool AllowOptional( IActivityMonitor monitor, string fullName );

    sealed class EmptyExcluder : IResPackageExcluder
    {
        public bool AllowOptional( IActivityMonitor monitor, Type type ) => true;

        public bool AllowOptional( IActivityMonitor monitor, string fullName ) => true;

        public bool AllowRequired( IActivityMonitor monitor, Type type ) => true;

        public bool AllowRequired( IActivityMonitor monitor, string fullName ) => true;
    }

    /// <summary>
    /// Empty implementation that allows any type and any full name.
    /// </summary>
    public static readonly IResPackageExcluder Empty = new EmptyExcluder();
}
