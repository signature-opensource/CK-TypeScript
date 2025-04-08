using CK.TypeScript;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular;

/// <summary>
/// Required decoration of <see cref="NgComponent"/>.
/// <para>
/// A NgComponent is a <see cref="TypeScriptPackage"/>.
/// </para>
/// </summary>
public class NgComponentAttribute : TypeScriptPackageAttribute
{
    internal const string BaseActualAttributeTypeAssemblyQualifiedName = "CK.TS.Angular.Engine.NgComponentAttributeImpl, CK.TS.Angular.Engine";

    public NgComponentAttribute( [CallerFilePath] string? callerFilePath = null )
        : base( BaseActualAttributeTypeAssemblyQualifiedName, callerFilePath )
    {
    }

    private protected NgComponentAttribute( string actualAttributeTypeAssemblyQualifiedName,
                                            string? finalCallerFilePath )
    : base( actualAttributeTypeAssemblyQualifiedName, finalCallerFilePath )
    {
    }

    /// <summary>
    /// Gets or sets whether this component has a &lt;router-outlet/&gt;.
    /// When true, this component can be a <see cref="NgRoutedComponentAttribute.TargetComponent"/>.
    /// </summary>
    public bool HasRoutes { get; set; }

}
