using CK.Core;
using CK.StObj.TypeScript;

namespace CK.TS.Angular;

/// <summary>
/// Base class for Angular components. A <see cref="NGComponentAttribute"/> must decorate the final type.
/// </summary>
[CKTypeDefiner]
public abstract class NGComponent : TypeScriptPackage
{
}
