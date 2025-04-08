using CK.Core;
using CK.TypeScript;

namespace CK.TS.Angular;

/// <summary>
/// Base class for Angular components. A <see cref="NgComponentAttribute"/> must decorate the final type
/// that must be <c>sealed</c>: specializing NgComponent is not supported by design.
/// <para>
/// The type name must end with "Component".
/// </para>
/// </summary>
[Setup.AlsoRegisterType( typeof( AppComponent ) )]
// AlsoRegisterType will be replaced with CK.Core.RegisterCKType but this is not
// supported yet because of the current use of the old CK.StObj.Engine.
// [RegisterCKType( typeof( AppComponent ) )]
public abstract class NgComponent : TypeScriptPackage
{
}

