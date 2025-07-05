using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

public interface ITokenFilterOperatorSource
{
    /// <summary>
    /// Gets the necessarily valid <see cref="TokenFilter"/> of this source.
    /// </summary>
    TokenFilter Tokens { get; }

    /// <summary>
    /// Returns an enumerator on these filtered <see cref="Tokens"/>.
    /// </summary>
    /// <returns></returns>
    TokenFilterEnumerator CreateTokenEnumerator();

    /// <summary>
    /// Gets whether this source is the root: it covers all the source code tokens.
    /// </summary>
    [MemberNotNullWhen( false, nameof( Previous ), nameof( Operator ) )]
    bool IsRoot { get; }

    /// <summary>
    /// Gets whether this source is the root or is the last operator of a location operator.
    /// </summary>
    bool IsSyntaxBorder { get; }

    /// <summary>
    /// Gets the previous source.
    /// Null only when <see cref="IsRoot"/> is true.
    /// </summary>
    ITokenFilterOperatorSource? Previous { get; }

    /// <summary>
    /// Gets this source's operator.
    /// Null only when <see cref="IsRoot"/> is true.
    /// </summary>
    ITokenFilterOperator? Operator { get; }
}
