namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/> with
/// a single <see cref="IResourceGroupPackage"/> to which the decorated type belongs.
/// </summary>
/// <typeparam name="T">The package group.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class GroupsAttribute<T> : Attribute
    where T : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 2 <see cref="IResourceGroupPackage"/> to which the decorated type belongs.
/// </summary>
/// <typeparam name="T1">The first package group.</typeparam>
/// <typeparam name="T2">The second package group.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class GroupsAttribute<T1,T2> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 3 <see cref="IResourceGroupPackage"/> to which the decorated type belongs.
/// </summary>
/// <typeparam name="T1">The first package group.</typeparam>
/// <typeparam name="T2">The second package group.</typeparam>
/// <typeparam name="T3">The third package group.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class GroupsAttribute<T1, T2, T3> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
    where T3 : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 4 <see cref="IResourceGroupPackage"/> to which the decorated type belongs.
/// </summary>
/// <typeparam name="T1">The first package group.</typeparam>
/// <typeparam name="T2">The second package group.</typeparam>
/// <typeparam name="T3">The third package group.</typeparam>
/// <typeparam name="T4">The fourth package group.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class GroupsAttribute<T1, T2, T3, T4> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
    where T3 : IResourceGroupPackage
    where T4 : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 5 <see cref="IResourceGroupPackage"/> to which the decorated type belongs.
/// </summary>
/// <typeparam name="T1">The first package group.</typeparam>
/// <typeparam name="T2">The second package group.</typeparam>
/// <typeparam name="T3">The third package group.</typeparam>
/// <typeparam name="T4">The fourth package group.</typeparam>
/// <typeparam name="T5">The fifth package group.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class GroupsAttribute<T1, T2, T3, T4, T5> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
    where T3 : IResourceGroupPackage
    where T4 : IResourceGroupPackage
    where T5 : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 6 <see cref="IResourceGroupPackage"/> to which the decorated type belongs.
/// </summary>
/// <typeparam name="T1">The first package group.</typeparam>
/// <typeparam name="T2">The second package group.</typeparam>
/// <typeparam name="T3">The third package group.</typeparam>
/// <typeparam name="T4">The fourth package group.</typeparam>
/// <typeparam name="T5">The fifth package group.</typeparam>
/// <typeparam name="T5">The sixth package group.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class GroupsAttribute<T1, T2, T3, T4, T5, T6> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
    where T3 : IResourceGroupPackage
    where T4 : IResourceGroupPackage
    where T5 : IResourceGroupPackage
    where T6 : IResourceGroupPackage
{
}
