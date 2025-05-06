using CK.Core;
using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    /// Initializes a new <see cref="Token"/>.
    /// </summary>
    /// <param name="tokenType">Type of the token. Must not be an error.</param>
    /// <param name="text">The token text.</param>
    /// <param name="leading">Leading trivias.</param>
    /// <param name="trailing">Trailing trivias.</param>
    public Token( TokenType tokenType, ImmutableArray<Trivia> leading, string text, ImmutableArray<Trivia> trailing )
    : this( tokenType, leading, text.AsMemory(), trailing )
    {
    }

    /// <summary>
    /// Initializes a new <see cref="Token"/> without leading trivias.
    /// </summary>
    /// <param name="tokenType">Type of the token. Must not be an error.</param>
    /// <param name="text">The token text.</param>
    /// <param name="trailing">Trailing trivias.</param>
    public Token( TokenType tokenType, string text, ImmutableArray<Trivia> trailing )
    : this( tokenType, Trivia.Empty, text.AsMemory(), trailing )
    {
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
    /// Compares the <see cref="Text"/>.
    /// </summary>
    /// <param name="text">The text to compare.</param>
    /// <param name="comparison">Comparison mode.</param>
    /// <returns>True if texts are equal, false otherwise.</returns>
    public bool TextEquals( ReadOnlySpan<char> text, StringComparison comparison = StringComparison.Ordinal )
    {
        return _text.Span.Equals( text, comparison );
    }

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

    /// <summary>
    /// Gets whether this is a detached trivia that has not been parsed: its <see cref="Text"/> is not a part
    /// of a wider string.
    /// </summary>
    public bool IsDetachedToken => GetContainingSpan(_text, out var start ).Length == _text.Length;

    /// <summary>
    /// Gets the 1-based column of this token in its containing text.
    /// <para>
    /// This really applies to tokens that have been parsed. For explicitly created token, this
    /// only analyze the leading trivias.
    /// </para>
    /// </summary>
    /// <returns>The 1-based column number of this token.</returns>
    public int GetColumnNumber()
    {
        int column = 1;
        ReadOnlySpan<char> span = default;
        int idx;
        // Analyze the leading trivias in reverse order to find first new line.
        for( int i = _leadingTrivias.Length - 1; i >= 0; i-- )
        {
            var t = _leadingTrivias[i];
            // If it's a line comment, we are done.
            if( t.TokenType.IsTriviaLineComment() )
            {
                return column;
            }
            // Block comment or whitespace: we try to find the last \n in it.
            span = t.Content.Span;
            idx = span.LastIndexOf( '\n' );
            if( idx >= 0 )
            {
                if( idx > 0 && span[idx - 1] == '\r' ) ++idx;
                column += span.Length - idx;
                return column;
            }
            // No luck.
            column += span.Length;
        }
        // The leading trivias don't have a \n.
        span = GetContainingSpan( _text, out var start );
        // Skip the leading trivias work (useless to reprocess them).
        start -= column - 1;
        // If start is before the containing span, this is a detached token
        // and we have nothing to do.
        if( start > 0 )
        {
            span = span.Slice( 0, start );
            idx = span.LastIndexOf( '\n' );
            if( idx >= 0 )
            {
                if( idx > 0 && span[idx - 1] == '\r' ) ++idx;
                column += span.Length - idx;
            }
        }
        return column;
    }

    /// <summary>
    /// Gets the <see cref="SourcePosition"/> of this token in its containg text.
    /// <para>
    /// This really applies to tokens that have been parsed. For explicitly created token, this
    /// only analyze the leading trivias.
    /// </para>
    /// </summary>
    /// <returns>The source position of this token.</returns>
    public SourcePosition GetSourcePosition()
    {
        var s = GetContainingSpan( _text, out var start );
        // Special handling for detached tokens.
        if( s.Length == _text.Length )
        {
            return _leadingTrivias.IsEmpty
                    ? new SourcePosition( 1, 1 )
                    : GetDetachedTokenSourcePosition();
        }
        return SourcePosition.GetSourcePosition( s, start );
    }

    SourcePosition GetDetachedTokenSourcePosition()
    {
        Throw.DebugAssert( _leadingTrivias.Length > 0 );
        int column = 1;
        int line = 1;
        bool knownColum = false;
        int idx;
        Trivia t = _leadingTrivias[_leadingTrivias.Length - 1];
        ReadOnlySpan<char> span = t.Content.Span;
        if( t.TokenType.IsTriviaLineComment() )
        {
            knownColum = true;
            ++line;
        }
        else
        {
            // Block comment or whitespace: we try to find the last \n in it.
            idx = span.LastIndexOf( '\n' );
            if( idx >= 0 )
            {
                column += span.Length - idx;
                if( idx > 0 && span[idx - 1] == '\r' ) --column;
                knownColum = true;
                line += 1 + span.Slice( 0, idx ).Count( '\n' );
            }
            else
            {
                column += span.Length;
            }
        }
        for( int i = _leadingTrivias.Length - 2; i >= 0; i-- )
        {
            t = _leadingTrivias[i];
            span = t.Content.Span;
            if( t.TokenType.IsTriviaLineComment() )
            {
                ++line;
                knownColum = true;
            }
            else
            {
                if( !knownColum )
                {
                    idx = span.LastIndexOf( '\n' );
                    if( idx >= 0 )
                    {
                        column += span.Length - idx;
                        if( idx > 0 && span[idx - 1] == '\r' ) --column;
                        knownColum = true;
                        ++line;
                        span = span.Slice( 0, idx );
                    }
                    else
                    {
                        column += span.Length;
                    }
                }
                line += span.Count( '\n' );
            }
        }
        return new SourcePosition( line, column );
    }

    internal static ReadOnlySpan<char> GetContainingSpan( ReadOnlyMemory<char> text, out int start )
    {
        if( MemoryMarshal.TryGetString( text, out var str, out start, out _ ) )
        {
            return str;
        }
        Throw.InvalidOperationException( "Token is not backed by a string." );
        return default;
    }

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
