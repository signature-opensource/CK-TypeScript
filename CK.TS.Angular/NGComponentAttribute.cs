using CK.TypeScript;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular;


/// <summary>
/// Non generic base class of <see cref="NgComponentAttribute{T}"/>.
/// </summary>
/// <typeparam name="T">The package to which this component belongs.</typeparam>
public class NgComponentAttribute : TypeScriptPackageAttribute
{
    internal const string BaseActualAttributeTypeAssemblyQualifiedName = "CK.TS.Angular.Engine.NgComponentAttributeImpl, CK.TS.Angular.Engine";

    internal protected NgComponentAttribute( [CallerFilePath] string? callerFilePath = null )
        : base( BaseActualAttributeTypeAssemblyQualifiedName, callerFilePath )
    {
    }

    private protected NgComponentAttribute( string actualAttributeTypeAssemblyQualifiedName,
                                            string? finalCallerFilePath,
                                            bool disableResources = false )
    : base( actualAttributeTypeAssemblyQualifiedName, finalCallerFilePath, disableResources )
    {
    }


    // Internal: Only AppComponent uses this to disableResources.
    internal NgComponentAttribute( bool disableResources,
                                   string actualAttributeTypeAssemblyQualifiedName,
                                   [CallerFilePath]string? finalCallerFilePath = null )
        : base( actualAttributeTypeAssemblyQualifiedName, finalCallerFilePath, disableResources )
    {
    }

    /// <summary>
    /// Gets or sets whether this component has a &lt;router-outlet/&gt;.
    /// When true, this component can be a <see cref="NgRoutedComponentAttribute.TargetComponent"/>.
    /// </summary>
    public bool HasRoutes { get; set; }

}
