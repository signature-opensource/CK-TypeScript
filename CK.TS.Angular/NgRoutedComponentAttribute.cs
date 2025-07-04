using System;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular;

/// <summary>
/// Required attribute for <see cref="NgRoutedComponent"/>.
/// </summary>
public class NgRoutedComponentAttribute : NgComponentAttribute
{
    /// <summary>
    /// Initializes a new <see cref="NgComponentAttribute"/>.
    /// </summary>
    /// <param name="targetRoutedComponent">See <see cref="TargetComponent"/>.</param>
    /// <param name="callerFilePath">The caller path.</param>
    public NgRoutedComponentAttribute( Type targetRoutedComponent,
                                       [CallerFilePath] string? callerFilePath = null )
        : base( "CK.TS.Angular.Engine.NgRoutedComponentAttributeImpl, CK.TS.Angular.Engine", callerFilePath )
    {
        TargetComponent = targetRoutedComponent;
    }

    /// <summary>
    /// Gets the component type under which this component must appear.
    /// <para>
    /// This can be the <see cref="CKGenAppModule"/> or a <see cref="NgComponent"/> with a true <see cref="NgComponentAttribute.HasRoutes"/> or
    /// another <see cref="NgRoutedComponent"/>. 
    /// </para>
    /// </summary>
    public Type TargetComponent { get; }

    /// <summary>
    /// Gets or sets the route for this component.
    /// <para>
    /// When let to null, the route name is the component name: the decorated type name in snake-case without the "Component" suffix.
    /// </para>
    /// </summary>
    public string? Route { get; set; }

    /// <summary>
    /// Gets or sets whether this component must be registered as a lazy loaded component.
    /// Defaults to <see cref="RouteRegistrationMode.None"/>.
    /// </summary>
    public RouteRegistrationMode RegistrationMode { get; set; }
}
