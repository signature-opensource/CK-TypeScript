namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/> with
/// a single child that is contained in the decorated type.
/// </summary>
/// <typeparam name="T">The child package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class ChildrenAttribute<T> : Attribute
    where T : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 2 children package that are contained by the decorated type.
/// </summary>
/// <typeparam name="T1">The first children package.</typeparam>
/// <typeparam name="T2">The second children package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class ChildrenAttribute<T1,T2> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 3 children package that are contained by the decorated type.
/// </summary>
/// <typeparam name="T1">The first children package.</typeparam>
/// <typeparam name="T2">The second children package.</typeparam>
/// <typeparam name="T3">The third children package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class ChildrenAttribute<T1, T2, T3> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
    where T3 : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 4 children package that are contained by the decorated type.
/// </summary>
/// <typeparam name="T1">The first children package.</typeparam>
/// <typeparam name="T2">The second children package.</typeparam>
/// <typeparam name="T3">The third children package.</typeparam>
/// <typeparam name="T4">The fourth children package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class ChildrenAttribute<T1, T2, T3, T4> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
    where T3 : IResourceGroupPackage
    where T4 : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 5 children package that are contained by the decorated type.
/// </summary>
/// <typeparam name="T1">The first children package.</typeparam>
/// <typeparam name="T2">The second children package.</typeparam>
/// <typeparam name="T3">The third children package.</typeparam>
/// <typeparam name="T4">The fourth children package.</typeparam>
/// <typeparam name="T5">The fifth children package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class ChildrenAttribute<T1, T2, T3, T4, T5> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
    where T3 : IResourceGroupPackage
    where T4 : IResourceGroupPackage
    where T5 : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 6 children package that are contained by the decorated type.
/// </summary>
/// <typeparam name="T1">The first children package.</typeparam>
/// <typeparam name="T2">The second children package.</typeparam>
/// <typeparam name="T3">The third children package.</typeparam>
/// <typeparam name="T4">The fourth children package.</typeparam>
/// <typeparam name="T5">The fifth children package.</typeparam>
/// <typeparam name="T5">The sixth children package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class ChildrenAttribute<T1, T2, T3, T4, T5, T6> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
    where T3 : IResourceGroupPackage
    where T4 : IResourceGroupPackage
    where T5 : IResourceGroupPackage
    where T6 : IResourceGroupPackage
{
}
