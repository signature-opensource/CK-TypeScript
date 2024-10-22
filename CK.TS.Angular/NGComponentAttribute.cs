using CK.StObj.TypeScript;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular;

/// <summary>
/// Required decoration of <see cref="NgComponent"/>.
/// </summary>
public class NgComponentAttribute : TypeScriptPackageAttribute
{
    /// <summary>
    /// Initializes a new <see cref="NgComponentAttribute"/>.
    /// </summary>
    /// <param name="callerFilePath">Automatically set by the Roslyn compiler and used to compute the associated embedded resource folder.</param>
    public NgComponentAttribute( [CallerFilePath] string? callerFilePath = null )
        : base( "CK.TS.Angular.Engine.NGComponentAttributeImpl, CK.TS.Angular.Engine", callerFilePath )
    {
    }

    /// <summary>
    /// Initializes a new specialized <see cref="NgComponentAttribute"/>.
    /// </summary>
    /// <param name="actualAttributeTypeAssemblyQualifiedName">Assembly Qualified Name of the object that will replace this attribute during setup.</param>
    /// <param name="finalCallerFilePath">Specialized types must provide the <c>[CallerFilePath]string? callerFilePath = null</c>.</param>
    protected NgComponentAttribute( string actualAttributeTypeAssemblyQualifiedName, string? finalCallerFilePath )
        : base( actualAttributeTypeAssemblyQualifiedName, finalCallerFilePath )
    {
    }

}
