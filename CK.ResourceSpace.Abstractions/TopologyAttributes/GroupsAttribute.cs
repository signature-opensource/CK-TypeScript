namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/> with
/// any number of <see cref="IResourceGroup"/> package full names to which the decorated
/// type belongs. Each string can be a single package full name or comma separated multiple
/// package full names. 
/// <para>
/// The typed attributes (<see cref="GroupsAttribute{T}"/>, ..., <see cref="GroupsAttribute{T1, T2, T3, T4, T5, T6}"/>)
/// should be preferred when the types of the groups are accessible.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class GroupsAttribute : Attribute
{
    /// <summary>
    /// Initializes a new set of <see cref="IResourceGroup"/> that contains the decorated type.
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
    /// Gets the set of <see cref="IResourceGroup"/> full names.
    /// </summary>
    public string[] CommaSeparatedPackageFullnames { get; }
}

