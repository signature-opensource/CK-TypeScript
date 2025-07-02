namespace CK.Core;

/// <summary>
/// Categorizes how a "component" (typically a Type but this can be applied to other kind of concept)
/// is registered in a registrar.
/// <para>
/// This has been designed to be as simple as possible and combinable, the greater wins:
/// <c>Excluded &gt; Required &gt; Regular &gt; None</c>.
/// </para>
/// <para>
/// Whether the exclusion propagates to other components that requires it or raises an error
/// is out of the scope of this categorization. Similarily, exclusion in a graph must be considered
/// "weak": structural requirements like the base type of type ignore exclusion. To exclude a Type that
/// has a specialization, both the specialization and the base type must be excluded.
/// </para>
/// </summary>
public enum RegistrationMode
{
    /// <summary>
    /// The component is not registered. It should be ignored unless required by another registered component.
    /// </summary>
    None,

    /// <summary>
    /// The component is simply registered: if it is an opt-in component and no other component require it, it will
    /// not be considered.
    /// </summary>
    Regular,

    /// <summary>
    /// The component is explicitly registered, even if it is an opt-in component, it will be considered.
    /// </summary>
    Required,

    /// <summary>
    /// The component is excluded and will not be considered at all.
    /// </summary>
    Excluded
}
