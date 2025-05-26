using CK.TypeScript;

namespace CK.TS.Angular;

/// <summary>
/// Base class for Angular components. A <see cref="NgComponentAttribute"/> must decorate the final type.
/// Direct specializations must be either <c>abstract</c> or <c>sealed</c>: specializing concrete
/// NgComponent is not supported by design.
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

