using CK.Core;
using CK.TypeScript;

namespace CK.TS.Angular;

/// <summary>
/// Base class for NgModule definition. A <see cref="NgModuleAttribute"/> must decorate the final type
/// that must be <c>sealed</c>: specializing NgModule is not supported by design.
/// Its type name must end with "Module".
/// <para>
/// NgModule are somehow deprecated. Angular standalone components are enough.
/// </para>
/// </summary>
[CKTypeDefiner]
public abstract class NgModule : TypeScriptPackage
{
}
