using CK.Core;

namespace CK.TypeScript;

/// <summary>
/// Base class for typescript packages.
/// <para>
/// Types that specialize this must be decorated by a <see cref="TypeScriptPackageAttribute"/> (or a specialization).
/// The [TypeScriptPackage] attribute provides the support of the folder of embedded resources associated to the package.
/// </para>
/// </summary>
[CKTypeDefiner]
public abstract class TypeScriptPackage : IRealObject
{
}
