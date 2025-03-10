namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/> with
/// any number of <see cref="IResourceGroupPackage"/> package full names to which the decorated
/// type belongs. Each string can be a single package full name or comma separated multiple
/// package full names. 
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class GroupsAttribute : Attribute
{
    /// <summary>
    /// Initializes a new set of <see cref="IResourceGroupPackage"/> that contains the decorated type.
    /// Each string can be a single package full name or comma separated multiple package full names. 
    /// </summary>
    /// <param name="commaSeparatedPackageFullnames">
    /// Each string can be a single package full name or comma separated multiple package full names.
    /// </param>
    public GroupsAttribute( params string[] commaSeparatedPackageFullnames )
    {
        CommaSeparatedPackageFullnames = commaSeparatedPackageFullnames;
    }

    /// <summary>
    /// Gets the set of <see cref="IResourceGroupPackage"/> full names.
    /// </summary>
    public string[] CommaSeparatedPackageFullnames { get; }
}

