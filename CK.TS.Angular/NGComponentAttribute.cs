using CK.StObj.TypeScript;
using System;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular;


/// <summary>
/// Non generic base class of <see cref="NgComponentAttribute{T}"/>.
/// </summary>
/// <typeparam name="T">The package to which this component belongs.</typeparam>
public class NgComponentAttribute : TypeScriptPackageAttribute
{
    internal protected NgComponentAttribute( [CallerFilePath] string? callerFilePath = null )
        : base( "CK.TS.Angular.Engine.NgComponentAttributeImpl, CK.TS.Angular.Engine", callerFilePath )
    {
    }

    private protected NgComponentAttribute( string actualAttributeTypeAssemblyQualifiedName, string? finalCallerFilePath )
        : base( actualAttributeTypeAssemblyQualifiedName, finalCallerFilePath )
    {
    }

    /// <summary>
    /// Gets or sets whether this component has a &lt;router-outlet/&gt;.
    /// When true, this component can be a <see cref="NgRoutedComponentAttribute.TargetComponent"/>.
    /// </summary>
    public bool HasRoutes { get; set; }

}
