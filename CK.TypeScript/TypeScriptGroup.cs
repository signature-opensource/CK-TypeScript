namespace CK.TypeScript;

/// <summary>
/// Base class for typescript groups.
/// <para>
/// Types that specialize this must be decorated by a <see cref="TypeScriptPackageAttribute"/> (or a specialization).
/// The [TypeScriptPackage] attribute provides the support of the "Res/" (and "Res[After/]" folders.
/// </para>
/// </summary>
public abstract class TypeScriptGroup : ITypeScriptGroup
{
    void ITypeScriptGroup.LocalImplementationOnly() {}
}
