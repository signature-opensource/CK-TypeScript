namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/> with
/// a single reverse requirement.
/// </summary>
/// <typeparam name="T">The package required by this one.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class RequiredByAttribute<T> : Attribute
    where T : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 2 reverse requirements.
/// </summary>
/// <typeparam name="T1">The first package required by this one.</typeparam>
/// <typeparam name="T2">The second package required by this one.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class RequiredByAttribute<T1,T2> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 3 reverse requirements.
/// </summary>
/// <typeparam name="T1">The first package required by this one.</typeparam>
/// <typeparam name="T2">The second package required by this one.</typeparam>
/// <typeparam name="T3">The third package required by this one.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class RequiredByAttribute<T1, T2, T3> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
    where T3 : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 4 reverse requirements.
/// </summary>
/// <typeparam name="T1">The first package required by this one.</typeparam>
/// <typeparam name="T2">The second package required by this one.</typeparam>
/// <typeparam name="T3">The third package required by this one.</typeparam>
/// <typeparam name="T4">The fourth package required by this one.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class RequiredByAttribute<T1, T2, T3, T4> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
    where T3 : IResourceGroupPackage
    where T4 : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 5 reverse requirements.
/// </summary>
/// <typeparam name="T1">The first package required by this one.</typeparam>
/// <typeparam name="T2">The second package required by this one.</typeparam>
/// <typeparam name="T3">The third package required by this one.</typeparam>
/// <typeparam name="T4">The fourth package required by this one.</typeparam>
/// <typeparam name="T5">The fifth package required by this one.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class RequiredByAttribute<T1, T2, T3, T4, T5> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
    where T3 : IResourceGroupPackage
    where T4 : IResourceGroupPackage
    where T5 : IResourceGroupPackage
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroupPackage"/> or <see cref="IResourcePackage"/>
/// with 6 reverse requirements.
/// </summary>
/// <typeparam name="T1">The first package required by this one.</typeparam>
/// <typeparam name="T2">The second package required by this one.</typeparam>
/// <typeparam name="T3">The third package required by this one.</typeparam>
/// <typeparam name="T4">The fourth package required by this one.</typeparam>
/// <typeparam name="T5">The fifth package required by this one.</typeparam>
/// <typeparam name="T5">The sixth package required by this one.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class RequiredByAttribute<T1, T2, T3, T4, T5, T6> : Attribute
    where T1 : IResourceGroupPackage
    where T2 : IResourceGroupPackage
    where T3 : IResourceGroupPackage
    where T4 : IResourceGroupPackage
    where T5 : IResourceGroupPackage
    where T6 : IResourceGroupPackage
{
}
