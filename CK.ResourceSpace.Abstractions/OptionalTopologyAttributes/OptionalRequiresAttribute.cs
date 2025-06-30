namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/> with
/// any number of package full names separated by commas or as independent strings
/// IF the full name belongs to the package selection.
/// <para>
/// The typed attributes (<see cref="OptionalRequiresAttribute{T}"/>, ..., <see cref="OptionalRequiresAttribute{T1, T2, T3, T4, T5, T6}"/>)
/// should be preferred when the types are accessible.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class OptionalRequiresAttribute : Attribute
{
    /// <summary>
    /// Initializes a new set of requirements. Each string can be a single package full name
    /// or comma separated multiple package full names. 
    /// </summary>
    /// <param name="commaSeparatedPackageFullnames">
    /// Each string can be a single package full name or comma separated multiple package full names.
    /// </param>
    public OptionalRequiresAttribute( params string[] commaSeparatedPackageFullnames )
    {
        CommaSeparatedPackageFullnames = commaSeparatedPackageFullnames;
    }

    /// <summary>
    /// Gets the set of requirements.
    /// </summary>
    public string[] CommaSeparatedPackageFullnames { get; }
}

