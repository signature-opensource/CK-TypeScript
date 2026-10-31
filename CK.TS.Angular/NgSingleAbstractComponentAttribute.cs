using CK.Core;
using System;

namespace CK.TS.Angular;

/// <summary>
/// Decorates an interface that is an abstract component: this is a <see cref="ISingleMappingAttribute"/>, at most one
/// <see cref="INgComponent"/> implementation is kept.
/// <para>
/// A reference to the abstract component (like <c>[Requires&lt;INgPrivatePageComponent&gt;]</c>) resolves to its
/// implementation: the only one registered as required, otherwise the only one registered. A configuration can then
/// choose its implementation among multiple ones.
/// </para>
/// <para>
/// The implementation's component name must be <see cref="ComponentName"/>.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Interface, AllowMultiple = false, Inherited = false )]
public sealed class NgSingleAbstractComponentAttribute : Attribute, ISingleMappingAttribute
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
