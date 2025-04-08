namespace CK.Core;

/// <summary>
/// Marks a type as being a <see cref="IResourceGroup"/> but restricts its Children to
/// not also belong to another <see cref="IResourcePackage"/>: a package is the single owner
/// of its children.
/// </summary>
public interface IResourcePackage : IResourceGroup
{
}
