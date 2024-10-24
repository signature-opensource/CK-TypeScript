namespace CK.TS.Angular;

/// <summary>
/// Defines how a <see cref="NgRoutedComponent"/> must be registered.
/// </summary>
public enum RouteRegistrationMode
{
    /// <summary>
    /// The routed component is eagerly loaded.
    /// </summary>
    None,

    /// <summary>
    /// The routed component is lazy loaded.
    /// </summary>
    Lazy
}

