using CK.StObj.TypeScript;
using System;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular;

public class RoutedComponentAttribute : TypeScriptPackageAttribute
{
    /// <summary>
    /// Initializes a new <see cref="RoutedComponentAttribute"/>.
    /// </summary>
    /// <param name="targetRoutedComponent">The routed component under which this component must appear.</param>
    /// <param name="route">The route for this component.</param>
    /// <param name="mode">Whether this component must be lazy loaded or not.</param>
    /// <param name="callerFilePath">Automatically set by the Roslyn compiler and used to compute the associated embedded resource folder.</param>
    public RoutedComponentAttribute( Type targetRoutedComponent,
                                     string route,
                                     RouteRegistrationMode mode = RouteRegistrationMode.None,
                                     [CallerFilePath] string? callerFilePath = null )
        : base( "CK.TS.Angular.Engine.RoutedComponentAttributeImpl, CK.TS.Angular.Engine", callerFilePath )
    {
        TargetRoutedComponent = targetRoutedComponent;
        Route = route;
        RegistrationMode = mode;
    }

    /// <summary>
    /// Gets the component type under which this component must appear.
    /// </summary>
    public Type TargetRoutedComponent { get; }

    /// <summary>
    /// Gets the route for this component.
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// Gets whether this component must be registered 
    /// </summary>
    public RouteRegistrationMode RegistrationMode { get; }

    /// <summary>
    /// Gets or sets an optional route in the <see cref="TargetRoutedComponent"/> under
    /// which this component's <see cref="Route"/> must appear.
    /// </summary>
    public string? AsChildOf { get; set; }
}
