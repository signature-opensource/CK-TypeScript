using CK.TypeScript;
using System;

namespace CK.TS.Angular;

/// <summary>
/// Decorates an interface that is an abstract component. There must be a single
/// <see cref="INgComponent"/> implementation in the set of registered types to setup.
/// <para>
/// Currently, multiple optional (<see cref="TypeScriptPackageAttribute.IsOptional"/>) implementations
/// is not handled: registered implementation must be unique (even if it is optional).
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Interface, AllowMultiple = false, Inherited = false )]
public sealed class NgSingleAbstractComponentAttribute : Attribute
{
    /// <summary>
    /// Initializes a new <see cref="NgSingleAbstractComponentAttribute"/>.
    /// </summary>
    /// <param name="componentName">The file name of the component ("private-page") without the ".component.ts" suffix.</param>
    public NgSingleAbstractComponentAttribute( string componentName )
    {
        ComponentName = componentName;
    }

    /// <summary>
    /// Gets the implementation component name.
    /// </summary>
    public string ComponentName { get; }
}
