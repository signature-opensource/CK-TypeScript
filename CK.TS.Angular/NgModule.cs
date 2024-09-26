using CK.Core;
using CK.StObj.TypeScript;

namespace CK.TS.Angular;

/// <summary>
/// Base class for NgModule definition. A <see cref="NgModuleAttribute"/> must decorate the final type.
/// <para>
/// NgModule are somehow deprecated. Angular standalone components are enough.
/// </para>
/// </summary>
[CKTypeDefiner]
public abstract class NgModule : TypeScriptPackage
{
}
