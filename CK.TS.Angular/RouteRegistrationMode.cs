namespace CK.TS.Angular;

/// <summary>
/// Defines how a <see cref="RoutedComponent"/> must be registered.
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

