namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/> with
/// any number of other package full names that the decorated type contains.
/// Each string can be a single package full name or comma separated multiple
/// package full names. 
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class ChildrenAttribute : Attribute
{
    /// <summary>
    /// Initializes a new set of package full names contained by the decorated type.
    /// Each string can be a single package full name or comma separated multiple package full names. 
    /// </summary>
    /// <param name="commaSeparatedPackageFullnames">
    /// Each string can be a single package full name or comma separated multiple package full names.
    /// </param>
    public ChildrenAttribute( params string[] commaSeparatedPackageFullnames )
    {
        CommaSeparatedPackageFullnames = commaSeparatedPackageFullnames;
    }

    /// <summary>
    /// Gets the set of <see cref="IResourceGroupPackage"/> full names.
    /// </summary>
    public string[] CommaSeparatedPackageFullnames { get; }
}

