using CK.Core;

namespace CK.CrisLike;

/// <summary>
/// We cannot use ICommand and ICommand&lt;Result&gt; in TypeScript, generic name is unique (no cardinality).
/// On the TypeScript side it must be ICommand&lt;TResult = void&gt;.
/// </summary>
public interface ICommand : IAbstractCommand
{
}

/// <summary>
/// Mapped to the same ICommand&lt;TResult = void&gt; as <see cref="ICommand"/>.
/// </summary>
public interface ICommand<out TResult> : IAbstractCommand
{
    /// <summary>
    /// Trick to retrieve the nullability information of the generic parameter.
    /// </summary>
    [AutoImplementationClaim]
    public static TResult TResultType => default!;
}
