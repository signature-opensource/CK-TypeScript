using CK.Core;
using System;
using System.Collections.Immutable;

namespace CK.Transform.Core;

/// <summary>
/// Parsing head for comment trivias.
/// <para>
/// Micro parsers for <see cref="Trivia"/> are <see cref="TriviaParser"/> functions (typically extension
/// methods on this collector) that call <see cref="Accept(TokenType, int)"/> or <see cref="EndOfInput(TokenType)"/>.
/// </para>
/// </summary>
public ref struct TriviaHead
{
    ReadOnlySpan<char> _head;
    readonly ImmutableArray<Trivia>.Builder _collector;
    readonly ReadOnlyMemory<char> _text;
    int _idxText;
    int _length;

    /// <summary>
    /// Initializes a new head on a text.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="collector">The collector of <see cref="Trivia"/>.</param>
    /// <param name="start">The starting index to consider in the text.</param>
    public TriviaHead( ref ReadOnlyMemory<char> text, ImmutableArray<Trivia>.Builder collector, int start = 0 )
    {
        _head = text.Span.Slice( start );
        _idxText = start;
        _text = text;
        _collector = collector;
    }

    internal TriviaHead( ReadOnlySpan<char> head, ReadOnlyMemory<char> text, ImmutableArray<Trivia>.Builder collector )
    {
        Throw.DebugAssert( text.Length >= head.Length );
        _head = head;
        _idxText = text.Length - head.Length;
        _text = text;
        _collector = collector;
    }

    /// <summary>
    /// Gets the head to analyze.
    /// </summary>
    public readonly ReadOnlySpan<char> Head => _head;

    /// <summary>
    /// Gets the accepted length (that is sum of the collected <see cref="Trivia.Content"/>'s length)
    /// regardless of the <see cref="Error"/>.
    /// </summary>
    public readonly int Length => _length;

    /// <summary>
    /// Gets whether head is empty.
    /// </summary>
    public readonly bool IsEndOfInput => _head.Length == 0;

    /// <summary>
    /// Accepts a trivia. This must not be called once <see cref="EndOfInput(TokenType)"/> has been called.
    /// </summary>
    /// <param name="tokenType">The type of trivia.</param>
    /// <param name="length">The trivia length. Must be positive.</param>
    public void Accept( TokenType tokenType, int length )
    {
        Throw.CheckState( IsEndOfInput is false );
        Throw.CheckArgument( tokenType.IsTrivia() );
        Throw.CheckArgument( length > 0 );
        _collector.Add( new Trivia( tokenType, _text.Slice( _idxText, length ) ) );
        _idxText += length;
        _length += length;
        _head = _head.Slice( length );
    }

    /// <summary>
    /// Signals an unterminated comment. The last <see cref="Trivia.TokenType"/> is the combination of 
    /// <see cref="TokenType.ErrorClassBit"/> and <paramref name="tokenType"/>.
    /// <para>
    /// This must be called only once. <see cref="Accept(TokenType, int)"/> cannot be called anymore.
    /// </para>
    /// </summary>
    /// <param name="tokenType">The type of trivia.</param>
    public void EndOfInput( TokenType tokenType )
    {
        Throw.CheckState( IsEndOfInput is false );
        Throw.CheckArgument( tokenType.IsTrivia() );
        _collector.Add( new Trivia( TokenType.ErrorClassBit | tokenType, _text.Slice( _idxText, _head.Length ) ) );
        _idxText += _head.Length;
        _length += _head.Length;
        _head = default;
    }

    /// <summary>
    /// Collects as many possible <see cref="Trivia"/>.
    /// <para>
    /// Whitespaces (span of <see cref="char.IsWhiteSpace(char)"/>) are automatically handled
    /// and collected as <see cref="TokenType.Whitespace"/>.
    /// </para>
    /// </summary>
    /// <param name="parser">The parser function. When null, only whitespaces are collected.</param>
    public void ParseAll( TriviaParser? parser )
    {
        if( IsEndOfInput ) return;
        for(; ; )
        {
            // A leading trivia eats all the whitespaces.
            ParseWhiteSpaces();
            if( IsEndOfInput ) return;
            int currentLength = _length;
            parser?.Invoke( ref this );
            if( IsEndOfInput || currentLength == _length )
            {
                break;
            }
        }
    }

    void ParseWhiteSpaces()
    {
        Throw.DebugAssert( !IsEndOfInput );
        if( char.IsWhiteSpace( _head[0] ) )
        {
            int iS = 0;
            while( ++iS != _head.Length && char.IsWhiteSpace( _head[iS] ) ) ;
            Accept( TokenType.Whitespace, iS );
        }
    }

    /// <summary>
    /// Collects the trivia that can be parsed by one of the <paramref name="parsers"/>.
    /// <para>
    /// This starts by collecting a <see cref="TokenType.Whitespace"/> trivia, then each
    /// parser is called (in the order of the <paramref name="parsers"/>). The first one
    /// that sets an error or accepts a trivia stops the enumeration.
    /// </para>
    /// </summary>
    /// <param name="parser">The parser function. When empty, only whitespaces are collected.</param>
    public void ParseAny( params ImmutableArray<TriviaParser> parsers )
    {
        if( IsEndOfInput ) return;
        ParseWhiteSpaces();
        foreach( var parser in parsers ) 
        {
            if( IsEndOfInput ) return;
            int currentLength = _length;
            parser( ref this );
            if( currentLength != _length )
            {
                break;
            }
        }
    }

    /// <summary>
    /// Collects either whitespaces up to a new line (included) or "pure" whitespaces and at most one trivia.
    /// <para>
    /// The <paramref name="parser"/> function should not accept more than one trivia.
    /// Regular trailing trivias immediately follow a token up to the end of the line.
    /// The trivias that start the next line belong to the next token.
    /// </para>
    /// </summary>
    /// <param name="parser">The parser function. When null, only whitespaces are collected.</param>
    public void ParseTrailingTrivias( TriviaParser? parser )
    {
        if( IsEndOfInput ) return;
        // A trailing trivia stops at the end of line...
        if( char.IsWhiteSpace( _head[0] ) )
        {
            bool eol = false;
            int iS = 0;
            while( ++iS != _head.Length && char.IsWhiteSpace( _head[iS] ) )
            {
                if( _head[iS] == '\n' )
                {
                    iS++;
                    eol = true;
                    break;
                }
            }
            Accept( TokenType.Whitespace, iS );
            if( eol || IsEndOfInput ) return;
        }
        // ...or consider only one comment.
        parser?.Invoke( ref this );
    }

}
