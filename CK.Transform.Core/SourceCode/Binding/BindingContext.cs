using CK.Core;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

/// <summary>
/// Provides the <see cref="Tokens"/> and a way to signal errors to <see cref="SourceSpan.Bind"/>.
/// </summary>
public ref struct BindingContext
{
    readonly ReadOnlySpan<Token> _tokens;
    ref List<BindingError>? _bindingErrors;
    readonly int _begSpan;

    internal BindingContext( ReadOnlySpan<Token> tokens,
                             ref List<BindingError>? bindingErrors,
                             int begSpan )
    {
        _tokens = tokens;
        _bindingErrors = ref bindingErrors;
        _begSpan = begSpan;
    }

    /// <summary>
    /// Gets the tokens of the span to be bound.
    /// </summary>
    public ReadOnlySpan<Token> SpanTokens => _tokens;

    /// <summary>
    /// Adds a <see cref="BindingError"/> at the <paramref name="culprit"/> position.
    /// </summary>
    /// <param name="culprit">The culprit.</param>
    /// <param name="message">The <see cref="TokenError.ErrorMessage"/>.</param>
    /// <param name="withTokenText">
    /// When true, the culprit's <see cref="Token.Text"/> is set as the token error text.
    /// When false, the <see cref="BindingError.Error"/>'s Text is left empty.
    /// </param>
    public void AddError( Token culprit, string message, bool withTokenText )
    {
        int idx = _tokens.IndexOf( culprit );
        Throw.CheckArgument( idx >= 0 );
        var e = new TokenError( TokenType.GenericError,
                                withTokenText ? culprit.Text : default,
                                message,
                                culprit.GetSourcePosition(),
                                Trivia.Empty,
                                Trivia.Empty );
        _bindingErrors ??= new List<BindingError>();
        _bindingErrors.Add( new BindingError( e, _begSpan + idx ) );
    }
}
