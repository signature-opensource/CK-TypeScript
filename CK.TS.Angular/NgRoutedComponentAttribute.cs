using System;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular;

/// <summary>
/// Required attribute for <see cref="NgRoutedComponent"/>.
/// </summary>
public class NgRoutedComponentAttribute : NgComponentAttribute
{
    /// <summary>
    /// Initializes a new <see cref="NgRoutedComponentAttribute"/>.
    /// </summary>
    /// <param name="targetRoutedComponent">The routed component under which this component must appear.</param>
    /// <param name="callerFilePath">Automatically set by the Roslyn compiler and used to compute the associated embedded resource folder.</param>
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
    /// When let to null, the route name is the component name: the decorated type name without the "Component" suffix in snake-case.
    /// </para>
    /// </summary>
    public string? Route { get; set; }

    /// <summary>
    /// Gets or sets whether this component must be registered as a lazy loaded component.
    /// Defaults to <see cref="RouteRegistrationMode.None"/>.
    /// </summary>
    public RouteRegistrationMode RegistrationMode { get; set; }
}
