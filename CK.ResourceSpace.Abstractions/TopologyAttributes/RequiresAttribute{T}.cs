namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/> with
/// a single requirement.
/// </summary>
/// <typeparam name="T">The required package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class RequiresAttribute<T> : Attribute
    where T : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 2 requirements.
/// </summary>
/// <typeparam name="T1">The first required package.</typeparam>
/// <typeparam name="T2">The second required package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class RequiresAttribute<T1,T2> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 3 requirements.
/// </summary>
/// <typeparam name="T1">The first required package.</typeparam>
/// <typeparam name="T2">The second required package.</typeparam>
/// <typeparam name="T3">The third required package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class RequiresAttribute<T1, T2, T3> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
    where T3 : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 4 requirements.
/// </summary>
/// <typeparam name="T1">The first required package.</typeparam>
/// <typeparam name="T2">The second required package.</typeparam>
/// <typeparam name="T3">The third required package.</typeparam>
/// <typeparam name="T4">The fourth required package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class RequiresAttribute<T1, T2, T3, T4> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
    where T3 : IResourceGroup
    where T4 : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 5 requirements.
/// </summary>
/// <typeparam name="T1">The first required package.</typeparam>
/// <typeparam name="T2">The second required package.</typeparam>
/// <typeparam name="T3">The third required package.</typeparam>
/// <typeparam name="T4">The fourth required package.</typeparam>
/// <typeparam name="T5">The fifth required package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class RequiresAttribute<T1, T2, T3, T4, T5> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
    where T3 : IResourceGroup
    where T4 : IResourceGroup
    where T5 : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 5 requirements.
/// </summary>
/// <typeparam name="T1">The first required package.</typeparam>
/// <typeparam name="T2">The second required package.</typeparam>
/// <typeparam name="T3">The third required package.</typeparam>
/// <typeparam name="T4">The fourth required package.</typeparam>
/// <typeparam name="T5">The fifth required package.</typeparam>
/// <typeparam name="T6">The sixth required package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class RequiresAttribute<T1, T2, T3, T4, T5, T6> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
    where T3 : IResourceGroup
    where T4 : IResourceGroup
    where T5 : IResourceGroup
    where T6 : IResourceGroup
{
}
