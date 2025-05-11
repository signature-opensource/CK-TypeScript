using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static CK.Transform.Core.SourceCodeEditor;

namespace CK.Transform.Core;

/// <summary>
/// Context for filtering tokens.
/// </summary>
public interface ITokenFilterBuilderContext
{
    /// <summary>
    /// Gets whether the <see cref="SourceCodeEditor"/> is on error.
    /// </summary>
    bool HasError { get; }

    /// <summary>
    /// Gets the current <see cref="SourceCode.Tokens"/>.
    /// </summary>
    IReadOnlyList<Token> UnfilteredTokens { get; }

    /// <summary>
    /// Gets the filtered tokens for this context.
    /// </summary>
    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Tokens { get; }

    /// <summary>
    /// Gets whether this context is the root: it covers all the source code tokens.
    /// </summary>
    [MemberNotNullWhen( false, nameof( Previous ) )]
    bool IsRoot { get; }

    /// <summary>
    /// Gets whether this context is the root or is the last one of a location filter.
    /// </summary>
    bool IsSyntaxBorder { get; }

    /// <summary>
    /// Gets the previous filtering context.
    /// Null only when <see cref="IsRoot"/> is true.
    /// </summary>
    ITokenFilterBuilderContext? Previous { get; }

    /// <summary>
    /// Gets this context's provider.
    /// Null only when <see cref="IsRoot"/> is true.
    /// </summary>
    IFilteredTokenEnumerableProvider? Provider { get; }

    /// <summary>
    /// Signals an error.
    /// <see cref="HasError"/> is set to true for all contexts, including the <see cref="SourceCodeEditor.HasError"/>.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    void Error( string errorMessage );

    /// <summary>
    /// Gets the deepest <see cref="SourceSpan"/> at a token position.
    /// </summary>
    /// <param name="index">The token index.</param>
    /// <returns>The deepest span or null.</returns>
    SourceSpan? GetDeepestSpanAt( int index );

    /// <summary>
    /// Gets the deepest span assignable to a <paramref name="spanType"/>.
    /// </summary>
    /// <param name="index">The token index.</param>
    /// <param name="spanType">Type of the span.</param>
    /// <returns>The deepest span or null.</returns>
    SourceSpan? GetDeepestSpanAt( int index, Type spanType );

    /// <summary>
    /// Gets the <see cref="SourceToken"/> of a <see cref="SourceSpan"/>.
    /// </summary>
    /// <param name="span">The span. <see cref="SourceSpan.IsDetached"/> must be false.</param>
    /// <returns>The source tokens.</returns>
    IEnumerable<SourceToken> GetSourceTokens( SourceSpan span );

    /// <summary>
    /// Creates a new <see cref="DynamicSpans"/>.
    /// </summary>
    /// <returns>A new <see cref="DynamicSpans"/>.</returns>
    DynamicSpans CreateDynamicSpan();


}
