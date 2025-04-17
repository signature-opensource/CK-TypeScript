namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/> to declare its
/// single <see cref="IResourcePackage"/> owner by its full type name.
/// <para>
/// The typed attribute <see cref="PackageAttribute{T}"/> should almost always be preferred.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
public sealed class PackageAttribute : Attribute
{
    /// <summary>
    /// Initializes a new <see cref="PackageAttribute"/>.
    /// </summary>
    /// <param name="packageFullName">The owner package's name.</param>
    public PackageAttribute( string packageFullName )
    {
        PackageFullName = packageFullName;
    }

    /// <summary>
    /// Gets the owner package's name.
    /// </summary>
    public string PackageFullName { get; }
}
