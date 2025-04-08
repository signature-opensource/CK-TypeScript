namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/> with
/// a single <see cref="IResourceGroup"/> to which the decorated type belongs.
/// </summary>
/// <typeparam name="T">The package group.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class GroupsAttribute<T> : Attribute
    where T : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 2 <see cref="IResourceGroup"/> to which the decorated type belongs.
/// </summary>
/// <typeparam name="T1">The first package group.</typeparam>
/// <typeparam name="T2">The second package group.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class GroupsAttribute<T1,T2> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 3 <see cref="IResourceGroup"/> to which the decorated type belongs.
/// </summary>
/// <typeparam name="T1">The first package group.</typeparam>
/// <typeparam name="T2">The second package group.</typeparam>
/// <typeparam name="T3">The third package group.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class GroupsAttribute<T1, T2, T3> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
    where T3 : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 4 <see cref="IResourceGroup"/> to which the decorated type belongs.
/// </summary>
/// <typeparam name="T1">The first package group.</typeparam>
/// <typeparam name="T2">The second package group.</typeparam>
/// <typeparam name="T3">The third package group.</typeparam>
/// <typeparam name="T4">The fourth package group.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class GroupsAttribute<T1, T2, T3, T4> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
    where T3 : IResourceGroup
    where T4 : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 5 <see cref="IResourceGroup"/> to which the decorated type belongs.
/// </summary>
/// <typeparam name="T1">The first package group.</typeparam>
/// <typeparam name="T2">The second package group.</typeparam>
/// <typeparam name="T3">The third package group.</typeparam>
/// <typeparam name="T4">The fourth package group.</typeparam>
/// <typeparam name="T5">The fifth package group.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class GroupsAttribute<T1, T2, T3, T4, T5> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
    where T3 : IResourceGroup
    where T4 : IResourceGroup
    where T5 : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 6 <see cref="IResourceGroup"/> to which the decorated type belongs.
/// </summary>
/// <typeparam name="T1">The first package group.</typeparam>
/// <typeparam name="T2">The second package group.</typeparam>
/// <typeparam name="T3">The third package group.</typeparam>
/// <typeparam name="T4">The fourth package group.</typeparam>
/// <typeparam name="T5">The fifth package group.</typeparam>
/// <typeparam name="T5">The sixth package group.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class GroupsAttribute<T1, T2, T3, T4, T5, T6> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
    where T3 : IResourceGroup
    where T4 : IResourceGroup
    where T5 : IResourceGroup
    where T6 : IResourceGroup
{
}
