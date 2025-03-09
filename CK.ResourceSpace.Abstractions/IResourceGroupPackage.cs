namespace CK.Core;

/// <summary>
/// Marks a type as being a package of resources. Such types must also be decorated
/// with at least one attribute that is a <see cref="IEmbeddedResourceTypeAttribute"/>.
/// <para>
/// a group package can contain any number of other <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// (its Children) that are free to also belong to other <see cref="IResourceGroupPackage"/>.
/// </para>
/// </summary>
public interface IResourceGroupPackage
{
}
