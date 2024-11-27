using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Threading;

namespace CK.Transform.Core;

/// <summary>
/// Abstract analyzer: <see cref="ParseTrivia(ref CK.Transform.Core.TriviaCollector)"/> and <see cref="ParseOne(ImmutableArray{CK.Transform.Core.Trivia}, ref ReadOnlyMemory{char})"/>
/// muts be implemented.
/// </summary>
public abstract class Analyzer
{
    ReadOnlyMemory<char> _text;
    readonly ImmutableArray<Trivia>.Builder _trivias;
    ReadOnlyMemory<char> _head;
    TokenErrorNode? _error;

    // Valid only during the Forward call.
    ImmutableArray<Trivia> _leadingTrivias;
    int _trailingTriviasResult;

    /// <summary>
    /// Initializes a new tokenizer.
    /// </summary>
    protected Analyzer()
    {
        _trivias = ImmutableArray.CreateBuilder<Trivia>();
    }

    /// <summary>
    /// Resets this tokenizer with a new string to tokenize.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    public virtual void Reset( ReadOnlyMemory<char> text )
    {
        _text = text;
        _trivias.Clear();
        _head = _text;
        _error = null;
    }

    /// <summary>
    /// Gets the whole text.
    /// </summary>
    public ReadOnlyMemory<char> Text => _text;

    /// <summary>
    /// Gets the not yet tokenized text.
    /// </summary>
    public ReadOnlyMemory<char> RemainingText => _head;

    /// <summary>
    /// Gets the error if any.
    /// Once an error is set, only <see cref="Reset(ReadOnlyMemory{char})"/> can be called.
    /// </summary>
    public TokenErrorNode? Error => _error;

    public ref struct Head
    {
        readonly Analyzer _analyzer;
        readonly ReadOnlySpan<char> _text;
        ReadOnlySpan<char> _head;

        internal Head( Analyzer analyzer )
        {
            _analyzer = analyzer;
            _text = analyzer._text.Span;
            _head = _text;
        }


    }

    ///// <summary>
    ///// Returns a node or a <see cref="NodeList{T}"/> of node.
    ///// </summary>
    ///// <returns></returns>
    //public IAbstractNode AnalyzeOneOrMore()
    //{
    //    var first = AnalyzeOne();
    //    if( first.TokenType.IsError() ) return first;

    //    var next = AnalyzeOne();
    //    TokenType t = next.TokenType;
    //    if( t.IsError() )
    //    {
    //        if( t == TokenType.ErrorUnhandled || t == TokenType.EndOfInput )
    //        {
    //            return first;
    //        }
    //        return new NodeList<AbstractNode>( [first,next], [], [] );
    //    }
    //    List<AbstractNode> multi = [first, next];
    //    for( ; ; )
    //    {
    //        next = AnalyzeOne();
    //        t = next.TokenType;
    //        if( t.IsError() )
    //        {
    //            if( t != TokenType.ErrorUnhandled && t != TokenType.EndOfInput )
    //            {
    //                multi.Add( first );
    //            }
    //            break;
    //        }
    //    }
    //    return new NodeList<AbstractNode>( multi, [], [] );
    //}

    /// <summary>
    /// Returns the next node. This handles the leading trivias, and calls the protected <see cref="ParseOne(ImmutableArray{Trivia}, ref ReadOnlyMemory{char})"/>
    /// that is the real analyzer.
    /// </summary>
    /// <returns>The next node.</returns>
    public IAbstractNode Parse()
    {
        Throw.CheckState( Error is null );
        int r = CollectLeadingTrivias();
        _leadingTrivias = _trivias.DrainToImmutable();
        if( r < 0 )
        {
            return CreateError( "Missing comment end.", (TokenType)r );
        }
        _head = _head.Slice( r );
        if( _head.Length == 0 )
        {
            return CreateError( "End of input.", TokenType.EndOfInput );
        }
        // We can use -1 for the unset result here because a
        // success is a positive value and an error is the combination
        // of TokenType.ClassErrorBit (the sign bit) and a TokenType:
        // a trivia result value cannot be full of 1 bits.
        _trailingTriviasResult = -1;
        var parsedNode = Parse( _leadingTrivias, ref _head );
        Throw.CheckState( "Parse() method returned null.", parsedNode != null );
        // Always update the head to expose an up-to-date RemaingText.
        if( _trailingTriviasResult > 0 )
        {
            _head = _head.Slice( _trailingTriviasResult );
        }
        Throw.DebugAssert( (parsedNode.TokenType < 0) == parsedNode is TokenErrorNode );

        if( parsedNode.TokenType < 0 ) return _error = SetErrorLocation( (TokenErrorNode)parsedNode );

        Throw.CheckState( GetTrailingTriviasAlreadyCalled is true );
        if( _trailingTriviasResult < 0 )
        {
            _error = new TokenErrorNode( (TokenType)r, $"Missing comment end after token '{parsedNode.GetType().Name}'.", CreateSourcePosition(), _leadingTrivias, parsedNode.TrailingTrivias );
            return SetErrorLocation( _error );
        }
        return parsedNode;
    }

    SourcePosition CreateSourcePosition()
    {
        int line, column;
        var sText = _text.Span;
        int headIndex = sText.Length - _head.Length;
        var before = sText.Slice( 0, headIndex );
        int lastIndex = before.LastIndexOf( '\n' );
        if( lastIndex >= 0 )
        {
            line = sText.Count( '\n' );
            if( lastIndex < sText.Length && sText[lastIndex] == '\r' ) ++lastIndex;
            column = headIndex - lastIndex;
            // Edge case: head is on the \r:
            if( column < 0 ) column = 0;
        }
        else
        {
            line = 0;
            column = headIndex;
        }
        return new SourcePosition( line, column );
    }

    /// <summary>
    /// Tries to read a top-level language node from the <see cref="RemainingText"/>. On success, the trailing trivias MUST be obtained by calling the protected
    /// <see cref="GetTrailingTrivias()"/> method.
    /// <para>
    /// When this analyzer doesn't recognize the language (typically because the very first token is unknwon), it must return a <see cref="TokenErrorNode.Unhandled"/>.
    /// </para>
    /// <para>
    /// The notion of "top-level" is totally language dependent. A language can perfectly decide that a list of statements must be handled
    /// as a top-level node. However, it is recommended that such "aggregates" be managed by provided <see cref="AnalyzeOneOrMore"/> that supports
    /// the combination of multiple top-level nodes into a <see cref="NodeList{T}"/> of <see cref="AbstractNode"/>.
    /// </para>
    /// </summary>
    /// <param name="leadingTrivias">The leading trivias of the token.</param>
    /// <param name="head">The current <see cref="RemainingText"/> that must be forwarded.</param>
    /// <returns>The node (can be a <see cref="TokenErrorNode"/>).</returns>
    internal protected abstract IAbstractNode Parse( ImmutableArray<Trivia> leadingTrivias, ref ReadOnlyMemory<char> head );

    int CollectLeadingTrivias()
    {
        var s = _head.Span;
        var c = new TriviaCollector( s, _head, _trivias );

        int currentResult = 0;
        for(; ; )
        {
            int iS = 0;
            // A leading trivia eats all the whitespaces. 
            while( ++iS != c.Head.Length && char.IsWhiteSpace( c.Head[iS] ) ) ;
            currentResult = c.Accept( TokenType.Whitespace, iS );
            ParseTrivia( ref c );
            if( c.Result < 0 ) return c.Result;
            if( currentResult == c.Result ) return currentResult;
            currentResult = c.Result;
        }
    }

    bool GetTrailingTriviasAlreadyCalled => _trailingTriviasResult != -1;

    /// <summary>
    /// Gets the trailing trivias. This MUST be called on success to build the resulting node. 
    /// </summary>
    /// <returns>The trailing trivias.</returns>
    internal protected ImmutableArray<Trivia> GetTrailingTrivias()
    {
        Throw.CheckState( GetTrailingTriviasAlreadyCalled is false );
        _trailingTriviasResult = CollectTrailingTrivias();
        return _trivias.DrainToImmutable();
    }

    int CollectTrailingTrivias()
    {
        var s = _head.Span;
        var c = new TriviaCollector( s, _head, _trivias );

        // A trailing trivia stops at the end of line...
        bool eol = false;
        int iS = 0;
        while( ++iS != s.Length && char.IsWhiteSpace( s[iS] ) )
        {
            if( s[iS] == '\n' )
            {
                eol = true;
                break;
            }
        }
        if( eol ) return c.Accept( TokenType.Whitespace, iS );
        // ...or consider only one comment.
        ParseTrivia( ref c );
        return c.Result;
    }

    /// <summary>
    /// Must try to parse one of the suported trivias.
    /// </summary>
    /// <param name="c">The trivia collector.</param>
    protected abstract void ParseTrivia( ref TriviaCollector c );

    /// <summary>
    /// Error factory. This is the only way for <see cref="Parse(ImmutableArray{Trivia}, ref ReadOnlyMemory{char})"/> to return an error.
    /// <para>
    /// </para>
    /// </summary>
    /// <param name="errorMessage"></param>
    /// <param name="errorType"></param>
    /// <returns></returns>
    protected TokenErrorNode CreateError( string errorMessage, TokenType errorType = TokenType.SyntaxError )
    {
        return new TokenErrorNode( errorType, errorMessage, CreateSourcePosition(), _leadingTrivias, ImmutableArray<Trivia>.Empty );
    }

    /// <summary>
    /// Helper function for easy case that matches the start of the <see cref="RemainingText"/>
    /// and forwards it on success.
    /// </summary>
    /// <param name="type">The token type.</param>
    /// <param name="text">The text that must match the start of the <see cref="RemainingText"/>.</param>
    /// <param name="result">The non null TokenNode on success.</param>
    /// <param name="comparisonType">Optional comparison type.</param>
    /// <returns>True on success, false otherwise.</returns>
    internal protected bool TryCreateToken( TokenType type,
                                            ReadOnlySpan<char> text,
                                            [NotNullWhen(true)] out TokenNode? result,
                                            StringComparison comparisonType = StringComparison.Ordinal )
    {
        if( _head.Span.StartsWith( text, comparisonType ) )
        {
            result = CreateToken( type, text.Length );
            return true;
        }
        result = null;
        return false;
    }

    /// <summary>
    /// Creates a token of the <paramref name="type"/> and <paramref name="tokenLenght"/> from <see cref="RemainingText"/>
    /// and updates RemainingText accordingly.
    /// </summary>
    /// <param name="type">The <see cref="TokenNode.TokenType"/> to create.</param>
    /// <param name="tokenLenght">The length of the token.</param>
    /// <returns>The token node.</returns>
    internal protected TokenNode CreateToken( TokenType type, int tokenLenght )
    {
        TokenNode? result = new TokenNode( type, _head.Slice( 0, tokenLenght ), _leadingTrivias, GetTrailingTrivias() );
        _head = _head.Slice( tokenLenght );
        return result;
    }
}
