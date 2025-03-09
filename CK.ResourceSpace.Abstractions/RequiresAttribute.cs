namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/> with
/// any number of package full names separated by commas or as independent strings.
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class RequiresAttribute : Attribute
{
    public RequiresAttribute( params string[] commaSeparatedPackageFullnames )
    {
        CommaSeparatedPackageFullnames = commaSeparatedPackageFullnames;
    }

    public string[] CommaSeparatedPackageFullnames { get; }
}
