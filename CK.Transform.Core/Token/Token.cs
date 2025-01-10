using CK.Core;
using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace CK.Transform.Core;

/// <summary>
/// A token has a <see cref="TokenType"/>, <see cref="LeadingTrivias"/>, <see cref="Text"/> and <see cref="TrailingTrivias"/>.
/// <para>
/// This can be specialized.
/// </para>
/// </summary>
public class Token
{
    readonly ReadOnlyMemory<char> _text;
    // Not readonly for the CloneWithTrivias that uses MemberwiseClone so
    // no extra virtual/override is required.
    ImmutableArray<Trivia> _leadingTrivias;
    ImmutableArray<Trivia> _trailingTrivias;
    readonly TokenType _tokenType;

    /// <summary>
    /// Initializes a new <see cref="Token"/>.
    /// </summary>
    /// <param name="tokenType">Type of the token. Must not be an error.</param>
    /// <param name="text">The token text.</param>
    /// <param name="leading">Leading trivias.</param>
    /// <param name="trailing">Trailing trivias.</param>
    public Token( TokenType tokenType, ImmutableArray<Trivia> leading, ReadOnlyMemory<char> text, ImmutableArray<Trivia> trailing )
    {
        Throw.CheckArgument( !leading.IsDefault && !trailing.IsDefault );
        Throw.CheckArgument( !tokenType.IsError() && !tokenType.IsTrivia() );
        _tokenType = tokenType;
        _text = text;
        _leadingTrivias = leading;
        _trailingTrivias = trailing;
    }

    /// <summary>
    /// Special internal constructor for error and special tokens: <paramref name="tokenType"/> is not checked, <paramref name="text"/> can be empty.
    /// </summary>
    /// <param name="leading">Leading trivias.</param>
    /// <param name="trailing">Trailing trivias.</param>
    /// <param name="tokenType">Unchecked token type.</param>
    /// <param name="text">May be empty.</param>
    internal Token( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing, TokenType tokenType, ReadOnlyMemory<char> text )
    {
        _tokenType = tokenType;
        _text = text;
        _leadingTrivias = leading;
        _trailingTrivias = trailing;
    }

    /// <summary>
    /// Gets the token type.
    /// </summary>
    public TokenType TokenType => _tokenType;

    /// <summary>
    /// Gets the text.
    /// </summary>
    public ReadOnlyMemory<char> Text => _text;

    /// <summary>
    /// Gets the leading <see cref="Trivia"/>.
    /// </summary>
    public ImmutableArray<Trivia> LeadingTrivias => _leadingTrivias;

    /// <summary>
    /// Gets the leading <see cref="Trivia"/>.
    /// <para>
    /// By default, this is whitespaces up to a new line (included) or "pure" whitespaces and at most one trivia.
    /// </para>
    /// </summary>
    public ImmutableArray<Trivia> TrailingTrivias => _trailingTrivias;

    internal Token CloneForTrivias( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing )
    {
        Throw.DebugAssert( !leading.IsDefault );
        Throw.DebugAssert( !trailing.IsDefault );
        var c = Unsafe.As<Token>( MemberwiseClone() );
        c._leadingTrivias = leading;
        c._trailingTrivias = trailing;
        return c;
    }

    /// <summary>
    /// Gets the <see cref="Text"/> as a string.
    /// This should be used in debug session only (this allocates a string).
    /// </summary>
    /// <returns></returns>
    public override string ToString() => _text.ToString();
}
