namespace CK.Core;

/// <summary>
/// This interface must be implemented by attributes that defines a family of <see cref="IResourceGroup"/> (and/or <see cref="IResourcePackage"/>)
/// that CAN be optional. There should usually be at most one such attribute that decorate a Type but if there are mutiple ones, all of them
/// must have a true <see cref="IsOptional"/> for the package to be considered optional by default.
/// <para>
/// Such opt-in groups or packages must be explicitly registered as non-optional or required by another registered
/// component, otherwise they are ignored.
/// </para>
/// <para>
/// Engines that handles such optional types should ensure that this is applied to "implementation": there is no
/// such thing as an "optional abstraction", an abstraction is always available unless it has no registered "implementation".
/// </para>
/// </summary>
public interface IOptionalResourceGroupAttribute
{
    /// <summary>
    /// Gets whether the decorated type is initially optional.
    /// </summary>
    bool IsOptional { get; }
}
