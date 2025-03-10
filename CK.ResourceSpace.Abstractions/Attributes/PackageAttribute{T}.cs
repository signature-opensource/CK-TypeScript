namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/> to declare its
/// single <see cref="IResourcePackage"/> owner.
/// </summary>
/// <typeparam name="T">The package that owns the decorated <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
public sealed class PackageAttribute<T> : Attribute where T : IResourcePackage
{
}

