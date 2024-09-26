using CK.Core;
using CK.StObj.TypeScript;

namespace CK.TS.Angular;

/// <summary>
/// Base class for routed components. A <see cref="RoutedComponentAttribute"/> must decorate the final type.
/// <para>
/// This kind of component must have an associated <c>routes.ts</c> with a single default export of its routes.
/// </para>
/// </summary>
[CKTypeDefiner]
public abstract class RoutedComponent : TypeScriptPackage
{
}
