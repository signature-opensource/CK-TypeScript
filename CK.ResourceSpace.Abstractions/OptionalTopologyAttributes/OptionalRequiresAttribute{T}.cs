namespace CK.Core;

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/> with
/// a single requirement IF the target type belongs to the package selection.
/// </summary>
/// <typeparam name="T">The optional package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class OptionalRequiresAttribute<T> : Attribute
    where T : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 2 requirements IF the target type belongs to the package selection.
/// </summary>
/// <typeparam name="T1">The first optional package.</typeparam>
/// <typeparam name="T2">The second optional package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class OptionalRequiresAttribute<T1,T2> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 3 requirements IF the target type belongs to the package selection.
/// </summary>
/// <typeparam name="T1">The first optional package.</typeparam>
/// <typeparam name="T2">The second optional package.</typeparam>
/// <typeparam name="T3">The third optional package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class OptionalRequiresAttribute<T1, T2, T3> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
    where T3 : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 4 requirements IF the target type belongs to the package selection.
/// </summary>
/// <typeparam name="T1">The first optional package.</typeparam>
/// <typeparam name="T2">The second optional package.</typeparam>
/// <typeparam name="T3">The third optional package.</typeparam>
/// <typeparam name="T4">The fourth optional package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class OptionalRequiresAttribute<T1, T2, T3, T4> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
    where T3 : IResourceGroup
    where T4 : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 5 requirements IF the target type belongs to the package selection.
/// </summary>
/// <typeparam name="T1">The first optional package.</typeparam>
/// <typeparam name="T2">The second optional package.</typeparam>
/// <typeparam name="T3">The third optional package.</typeparam>
/// <typeparam name="T4">The fourth optional package.</typeparam>
/// <typeparam name="T5">The fifth optional package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class OptionalRequiresAttribute<T1, T2, T3, T4, T5> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
    where T3 : IResourceGroup
    where T4 : IResourceGroup
    where T5 : IResourceGroup
{
}

/// <summary>
/// Decorates a <see cref="IResourceGroup"/> or <see cref="IResourcePackage"/>
/// with 6 requirements IF the target type belongs to the package selection.
/// </summary>
/// <typeparam name="T1">The first optional package.</typeparam>
/// <typeparam name="T2">The second optional package.</typeparam>
/// <typeparam name="T3">The third optional package.</typeparam>
/// <typeparam name="T4">The fourth optional package.</typeparam>
/// <typeparam name="T5">The fifth optional package.</typeparam>
/// <typeparam name="T6">The sixth optional package.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class OptionalRequiresAttribute<T1, T2, T3, T4, T5, T6> : Attribute
    where T1 : IResourceGroup
    where T2 : IResourceGroup
    where T3 : IResourceGroup
    where T4 : IResourceGroup
    where T5 : IResourceGroup
    where T6 : IResourceGroup
{
}
