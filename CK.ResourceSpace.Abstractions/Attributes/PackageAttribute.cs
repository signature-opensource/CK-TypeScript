namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/> to declare its
/// single <see cref="IResourcePackage"/> owner by its full type name.
/// <para>
/// The typed attribute <see cref="PackageAttribute{T}"/> should almost always be preferred.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
public sealed class PackageAttribute : Attribute
{
    public PackageAttribute( string packageFullName )
    {
        PackageFullName = packageFullName;
    }

    public string PackageFullName { get; }
}
