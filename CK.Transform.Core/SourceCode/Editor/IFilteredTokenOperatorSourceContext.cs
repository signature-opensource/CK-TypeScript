using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

public interface IFilteredTokenOperatorSourceContext
{
    /// <summary>
    /// Gets the matched tokens for this context.
    /// </summary>
    IReadOnlyList<FilteredTokenSpan> FilteredTokens { get; }

    /// <summary>
    /// Gets whether this context is the root: it covers all the source code tokens.
    /// </summary>
    [MemberNotNullWhen( false, nameof( Previous ) )]
    bool IsRoot { get; }

    /// <summary>
    /// Gets whether this context is the root or is the last one of a location operator.
    /// </summary>
    bool IsSyntaxBorder { get; }

    /// <summary>
    /// Gets the previous filtering context.
    /// Null only when <see cref="IsRoot"/> is true.
    /// </summary>
    IFilteredTokenOperatorSourceContext? Previous { get; }

    /// <summary>
    /// Gets this context's operator.
    /// Null only when <see cref="IsRoot"/> is true.
    /// </summary>
    IFilteredTokenOperator? Operator { get; }

}
