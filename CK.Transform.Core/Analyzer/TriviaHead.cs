using CK.Core;
using System;
using System.Collections.Immutable;

namespace CK.Transform.Core;

/// <summary>
/// Parsing head for comment trivias.
/// <para>
/// 
/// </para>
/// Micro parsers for <see cref="Trivia"/> are extension methods on this collector
/// that call <see cref="Accept(NodeType, int)"/> or <see cref="Reject(NodeType)"/>.
/// <para>
/// </para>
/// </summary>
public ref struct TriviaHead
{
    ReadOnlySpan<char> _head;
    readonly ImmutableArray<Trivia>.Builder _collector;
    readonly ReadOnlyMemory<char> _text;
    int _idxText;
    int _acceptedLength;
    NodeType _error;

    /// <summary>
    /// Initializes a new head on a text.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="collector">The collector of <see cref="Trivia"/>.</param>
    /// <param name="start">The starting index to consider in the text.</param>
    public TriviaHead( ReadOnlyMemory<char> text, ImmutableArray<Trivia>.Builder collector, int start = 0 )
    {
        _head = text.Span.Slice( start );
        _idxText = start;
        _text = text;
        _collector = collector;
    }

    internal TriviaHead( ReadOnlySpan<char> head, int idxText, ReadOnlyMemory<char> text, ImmutableArray<Trivia>.Builder collector )
    {
        _head = head;
        _idxText = idxText;
        _text = text;
        _collector = collector;
    }

    /// <summary>
    /// Accepts a trivia. This must not be called once <see cref="Error(NodeType)"/> has been called.
    /// </summary>
    /// <param name="tokenType">The type of trivia.</param>
    /// <param name="length">The trivia length. Must be positive.</param>
    /// <returns>The <see cref="AcceptedLength"/>.</returns>
    public int Accept( NodeType tokenType, int length )
    {
        Throw.CheckState( HasError is false );
        Throw.CheckArgument( tokenType.IsTrivia() );
        Throw.CheckArgument( length > 0 );
        _collector.Add( new Trivia( tokenType, _text.Slice( _idxText, length ) ) );
        _idxText += length;
        _head = _head.Slice( length );
        return _acceptedLength += length;
    }

    /// <summary>
    /// Gets the accepted length (that is sum of the collected <see cref="Trivia.Content"/>'s length)
    /// regardless of the <see cref="Error"/>.
    /// </summary>
    public readonly int AcceptedLength => _acceptedLength;

    /// <summary>
    /// Gets the error sets by <see cref="Reject(NodeType)"/>.
    /// Defaults to <see cref="NodeType.None"/>.
    /// </summary>
    public readonly NodeType Error => _error;

    /// <summary>
    /// Gets whether <see cref="Reject(NodeType)"/> has been called.
    /// </summary>
    public readonly bool HasError => _error != NodeType.None;

    /// <summary>
    /// Signals an error by returning and setting the <see cref="Error"/> to the combination of 
    /// <see cref="NodeType.ErrorClassBit"/> and <paramref name="tokenType"/>.
    /// <para>
    /// This must be called only once. <see cref="Accept(NodeType, int)"/> cannot be called anymore.
    /// </para>
    /// </summary>
    /// <param name="tokenType">The type of trivia.</param>
    /// <returns>The error token type.</returns>
    public NodeType Reject( NodeType tokenType )
    {
        Throw.CheckState( HasError is false );
        Throw.CheckArgument( tokenType.IsTrivia() );
        return _error = NodeType.ErrorClassBit | tokenType;
    }

    /// <summary>
    /// Gets the head to analyze.
    /// </summary>
    public readonly ReadOnlySpan<char> Head => _head;

    /// <summary>
    /// Collects as many possible <see cref="Trivia"/>.
    /// <para>
    /// Whitespaces (span of <see cref="char.IsWhiteSpace(char)"/>) are automatically handled
    /// and collected as <see cref="NodeType.Whitespace"/>.
    /// </para>
    /// </summary>
    /// <param name="parser">The parser function. When null, only whitespaces are collected.</param>
    public void ParseAll( TriviaParser? parser )
    {
        for(; ; )
        {
            // A leading trivia eats all the whitespaces.
            ParseWhiteSpaces();
            int currentLength = _acceptedLength;
            parser?.Invoke( ref this );
            if( _error != NodeType.None || currentLength == _acceptedLength )
            {
                break;
            }
        }
    }

    void ParseWhiteSpaces()
    {
        if( char.IsWhiteSpace( _head[0] ) )
        {
            int iS = 0;
            while( ++iS != _head.Length && char.IsWhiteSpace( _head[iS] ) ) ;
            Accept( NodeType.Whitespace, iS );
        }
    }

    /// <summary>
    /// Collects the trivia that can be parsed by one of the <paramref name="parsers"/>.
    /// <para>
    /// This starts by collecting a <see cref="NodeType.Whitespace"/> trivia, then each
    /// parser is called (in the order of the <paramref name="parsers"/>). The first one
    /// that sets an error or accepts a trivia stops the enumeration.
    /// </para>
    /// </summary>
    /// <param name="parser">The parser function. When empty, only whitespaces are collected.</param>
    public void ParseAny( params ImmutableArray<TriviaParser> parsers )
    {
        ParseWhiteSpaces();
        foreach( var parser in parsers ) 
        {
            int currentLength = _acceptedLength;
            parser( ref this );
            if( _error != NodeType.None || currentLength != _acceptedLength )
            {
                break;
            }
        }
    }

    /// <summary>
    /// Collects either whitespaces up to a new line or "pure" whitespaces and at most one trivia.
    /// <para>
    /// The <paramref name="parser"/> function should not accept more than one trivia.
    /// Regular trailing trivias immediately follow a token up to the end of the line.
    /// The trivias that start the next line belong to the next token.
    /// </para>
    /// </summary>
    /// <param name="parser">The parser function. When null, only whitespaces are collected.</param>
    public void ParseTrailingTrivias( TriviaParser? parser )
    {
        // A trailing trivia stops at the end of line...
        if( char.IsWhiteSpace( _head[0] ) )
        {
            bool eol = false;
            int iS = 0;
            while( ++iS != _head.Length && char.IsWhiteSpace( _head[iS] ) )
            {
                if( _head[iS] == '\n' )
                {
                    eol = true;
                    break;
                }
            }
            Accept( NodeType.Whitespace, iS );
            if( eol ) return;
        }
        // ...or consider only one comment.
        parser?.Invoke( ref this );
    }

}
