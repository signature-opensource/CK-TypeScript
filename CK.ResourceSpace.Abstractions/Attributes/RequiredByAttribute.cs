namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/> with
/// any number of reverse dependencies thar are package full names separated by commas
/// or as independent strings.
/// <para>
/// The typed attributes (<see cref="RequiredByAttribute{T}"/>, ..., <see cref="RequiredByAttribute{T1, T2, T3, T4, T5, T6}"/>)
/// should be preferred when the types are accessible.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class RequiredByAttribute : Attribute
{
    /// <summary>
    /// Initializes a new set of reverse requirements. Each string can be a single package full name
    /// or comma separated multiple package full names. 
    /// </summary>
    /// <param name="commaSeparatedPackageFullnames">
    /// Each string can be a single package full name or comma separated multiple package full names.
    /// </param>
    public RequiredByAttribute( params string[] commaSeparatedPackageFullnames )
    {
        CommaSeparatedPackageFullnames = commaSeparatedPackageFullnames;
    }

    /// <summary>
    /// Gets the set of reverse requirements.
    /// </summary>
    public string[] CommaSeparatedPackageFullnames { get; }
}

