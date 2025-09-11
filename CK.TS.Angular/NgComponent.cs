using CK.Core;
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
[AlsoRegisterType<AppComponent>]
public abstract class NgComponent : TypeScriptPackage, INgComponent
{
    void INgComponent.LocalImplementationOnly() {}
}

