namespace CK.Core;

/// <summary>
/// Marks a type as being a greoup of resources. Such types must also be decorated
/// with at least one attribute that is a <see cref="IEmbeddedResourceTypeAttribute"/>.
/// <para>
/// a group package can contain any number of other <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// (its Children) that are free to also belong to other <see cref="IResourceGroup"/>.
/// </para>
/// </summary>
public interface IResourceGroup
{
}
