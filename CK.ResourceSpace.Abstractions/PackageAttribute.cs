namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/> to declare its
/// single <see cref="IResourcePackage"/> owner by its full type name.
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
