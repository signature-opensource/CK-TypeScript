using CK.StObj.TypeScript;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular;

/// <summary>
/// Required decoration of <see cref="NgModule"/>.
/// </summary>
public class NgModuleAttribute : TypeScriptPackageAttribute
{
    /// <summary>
    /// Initializes a new <see cref="NgModuleAttribute"/>.
    /// </summary>
    /// <param name="callerFilePath">Automatically set by the Roslyn compiler and used to compute the associated embedded resource folder.</param>
    public NgModuleAttribute( [CallerFilePath] string? callerFilePath = null )
        : base( "CK.TS.Angular.Engine.NgModuleAttributeImpl, CK.TS.Angular.Engine", callerFilePath )
    {
    }
}
