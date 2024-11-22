using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

public abstract class PartialTokenizer
{
    // Only the host can call Tokenize.
    [AllowNull] MultiTokenizer _host;
    bool _disabled;

    internal MultiTokenizer Host => _host;
    internal void SetHost( MultiTokenizer? host ) => _host = host;

    /// <summary>
    /// Gets or sets whether this partial tokenizer is temporarily disabled.
    /// Defaults to false.
    /// </summary>
    public bool IsDisabled
    {
        get => _disabled;
        set => _disabled = value;
    }

    /// <summary>
    /// Gets the type name by default.
    /// </summary>
    public virtual string Name => GetType().Name;

    /// <summary>
    /// Gets the whole text.
    /// </summary>
    protected ReadOnlyMemory<char> Text => _host.Text;

    /// <summary>
    /// Gets the not yet tokenized text.
    /// </summary>
    public ReadOnlyMemory<char> RemainingText => _host.RemainingText;

    /// <summary>
    /// Tries to read the next token or return the <see cref="TokenErrorNode.Unhandled"/>.
    /// <para>
    /// On success, the trailing trivias MUST be obtained by calling the protected
    /// <see cref="GetTrailingTrivias()"/> method.
    /// </para>
    /// </summary>
    /// <param name="leadingTrivias">The leading trivias of the token.</param>
    /// <param name="head">The current <see cref="RemainingText"/> that must be forwarded.</param>
    /// <returns>The token node (can be a <see cref="TokenErrorNode"/> or the <see cref="TokenErrorNode.Unhandled"/>).</returns>
    internal protected abstract TokenNode Tokenize( ImmutableArray<Trivia> leadingTrivias, ref ReadOnlyMemory<char> head );

    /// <summary>
    /// Gets the trailing trivias. This MUST be called on success to build the resulting token. 
    /// </summary>
    /// <returns>The trailing trivias.</returns>
    protected ImmutableArray<Trivia> GetTrailingTrivias() => _host.GetTrailingTrivias();

    /// <summary>
    /// Helper function for easy case that matches the start of the <see cref="RemainingText"/>
    /// and forwards it on success.
    /// </summary>
    /// <param name="tokenType">The <see cref="TokenNode.TokenType"/> to create.</param>
    /// <param name="text">The text that must match the start of the <see cref="RemainingText"/>.</param>
    /// <param name="result">The non null TokenNode on success.</param>
    /// <param name="comparisonType">Optional comparison type.</param>
    /// <returns>True on success, false otherwise.</returns>
    protected bool TryCreateToken( int tokenType,
                                   ReadOnlySpan<char> text,
                                   [NotNullWhen( true )] out TokenNode? result,
                                   StringComparison comparisonType = StringComparison.Ordinal )
    {
        return _host.TryCreateToken( (TokenType)tokenType, text, out result, comparisonType );
    }

    /// <summary>
    /// Creates a token of the <paramref name="type"/> and <paramref name="tokenLenght"/> from <see cref="RemainingText"/>
    /// and updates RemainingText accordingly.
    /// </summary>
    /// <param name="tokenType">The <see cref="TokenNode.TokenType"/> to create.</param>
    /// <param name="tokenLenght">The length of the token.</param>
    /// <returns>The token node.</returns>
    protected TokenNode CreateToken( int tokenType, int tokenLenght )
    {
        return _host.CreateToken( (TokenType)tokenType, tokenLenght );
    }

    public override string ToString() => Name;
}
