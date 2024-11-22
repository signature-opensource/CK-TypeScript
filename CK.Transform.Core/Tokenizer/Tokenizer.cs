using CK.Core;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace CK.Transform.Core;

abstract class Tokenizer
{
    ReadOnlyMemory<char> _text;
    readonly ImmutableArray<Trivia>.Builder _trivias;
    ReadOnlyMemory<char> _head;
    AllowedCommentsKind _allowedComments;

    // Valid only during the Forward call.
    ImmutableArray<Trivia> _leadingTrivias;
    int _trailingTriviasResult;

    /// <summary>
    /// Configures the <see cref="AllowedComments"/>.
    /// </summary>
    [Flags]
    public enum AllowedCommentsKind
    {
        /// <summary>
        /// No comments will be handled.
        /// </summary>
        None = 0,

        /// <summary>
        /// See <see cref="TokenType.LineComment"/>.
        /// </summary>
        LineComment = 1,

        /// <summary>
        /// See <see cref="TokenType.StarComment"/>.
        /// </summary>
        StarComment = 2,

        /// <summary>
        /// See <see cref="TokenType.SqlComment"/>.
        /// </summary>
        SqlComment = 4,

        /// <summary>
        /// See <see cref="TokenType.XmlComment"/>.
        /// </summary>
        XmlComment = 8
    }

    /// <summary>
    /// Initializes a new tokenizer.
    /// </summary>
    protected Tokenizer()
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
    /// Gets or sets the kind of comments that must be handled.
    /// This can be changed anytime.
    /// </summary>
    public AllowedCommentsKind AllowedComments
    {
        get => _allowedComments;
        set => _allowedComments = value;
    }

    /// <summary>
    /// Returns the next token until a <see cref="TokenErrorNode"/> is returned.
    /// </summary>
    /// <returns>The next token.</returns>
    public TokenNode Forward()
    {
        int r = CollectTrivias( _head, _allowedComments, true, _trivias );
        _leadingTrivias = _trivias.DrainToImmutable();
        if( r < 0 )
        {
            return SetErrorLocation( new TokenErrorNode( (TokenType)r, "Missing comment end.", _leadingTrivias ) );
        }
        _head = _head.Slice( r );
        if( _head.Length == 0 )
        {
            return SetErrorLocation( new TokenErrorNode( TokenType.EndOfInput, "End of input.", _leadingTrivias ) );
        }
        _trailingTriviasResult = -1;
        var t = Tokenize( _leadingTrivias, ref _head );
        // Always update the head to expose an up-to-date RemaingText.
        if( _trailingTriviasResult > 0 )
        {
            _head = _head.Slice( _trailingTriviasResult );
        }

        Throw.DebugAssert( (t.TokenType < 0) == t is TokenErrorNode );

        if( t.TokenType < 0 ) return SetErrorLocation( (TokenErrorNode)t );

        Throw.CheckState( GetTrailingTriviasAlreadyCalled is true );
        if( _trailingTriviasResult < 0 )
        {
            return SetErrorLocation( new TokenErrorNode( (TokenType)r, $"Missing comment end after token '{t.GetType().Name}'.", _leadingTrivias, t.TrailingTrivias ) );
        }
        return t;
    }

    TokenNode SetErrorLocation( TokenErrorNode error )
    {
        return error;
    }

    /// <summary>
    /// Must read the next token. On success, the trailing trivias MUST be obtained by calling the protected
    /// <see cref="GetTrailingTrivias()"/> method.
    /// </summary>
    /// <param name="leadingTrivias">The leading trivias of the token.</param>
    /// <param name="head">The current <see cref="RemainingText"/> that must be forwarded.</param>
    /// <returns>The token node (can be a <see cref="TokenErrorNode"/>).</returns>
    internal protected abstract TokenNode Tokenize( ImmutableArray<Trivia> leadingTrivias, ref ReadOnlyMemory<char> head );

    bool GetTrailingTriviasAlreadyCalled => _trailingTriviasResult != -1;

    /// <summary>
    /// Gets the trailing trivias. This MUST be called on success to build the resulting token. 
    /// </summary>
    /// <returns>The trailing trivias.</returns>
    internal protected ImmutableArray<Trivia> GetTrailingTrivias()
    {
        Throw.CheckState( GetTrailingTriviasAlreadyCalled is false );
        _trailingTriviasResult = CollectTrivias( _head, _allowedComments, true, _trivias );
        return _trivias.DrainToImmutable();
    }

    static int CollectTrivias( ReadOnlyMemory<char> head,
                               AllowedCommentsKind type,
                               bool isLeading,
                               ImmutableArray<Trivia>.Builder collector )
    {
        int iCurrent = 0;
        var s = head.Span;

        again:
        int iS = 0;
        if( s.Length == 0 )
        {
            Throw.DebugAssert( iCurrent == head.Length );
            return iCurrent;
        }
        if( char.IsWhiteSpace( s[iS] ) )
        {
            if( isLeading )
            {
                // A leading trivia eats all the whitespaces. 
                while( ++iS != s.Length && char.IsWhiteSpace( s[iS] ) ) ;
                collector.Add( new Trivia( TokenType.Whitespace, head.Slice( iCurrent, iS ) ) );
                iCurrent += iS;
                s = s.Slice( iS );
            }
            else
            {
                // A trailing trivia stops at the end of a line.
                while( ++iS != s.Length && char.IsWhiteSpace( s[iS] ) )
                {
                    if( s[iS] == '\n' )
                    {
                        if( ++iS != s.Length && s[iS] == '\r' ) ++iS;
                        break;
                    }
                }
                collector.Add( new Trivia( TokenType.Whitespace, head.Slice( iCurrent, iS ) ) );
                iCurrent += iS;
                return iCurrent;
            }
        }
        if( s[iS] == '/' )
        {
            if( ++iS == s.Length ) return iCurrent;
            if( s[iS] == '/' && (type & AllowedCommentsKind.LineComment) != 0 )
            {
                while( ++iS < s.Length && s[iS] != '\n' ) ;
                if( iS < s.Length && s[iS] == '\r' ) ++iS;
                collector.Add( new Trivia( TokenType.LineComment, head.Slice( iCurrent, iS ) ) );
                iCurrent += iS;
                // An end of line ends a trailing trivia.
                if( !isLeading ) return iCurrent;
                s = s.Slice( iS );
                goto again;
            }
            else if( s[iS] == '*' && (type & AllowedCommentsKind.StarComment) != 0 )
            {
                for(; ; )
                {
                    if( ++iS == s.Length ) return (int)TokenType.ErrorClosingStarComment;
                    if( s[iS] == '*' )
                    {
                        if( ++iS == s.Length ) return (int)TokenType.ErrorClosingStarComment;
                        if( s[iS] == '/' )
                        {
                            collector.Add( new Trivia( TokenType.StarComment, head.Slice( iCurrent, iS ) ) );
                            iCurrent += iS;
                            s = s.Slice( iS );
                            goto again;
                        }
                    }
                }
            }
            else
            {
                return iCurrent;
            }
        }
        if( s.StartsWith( "<!--" ) && (type & AllowedCommentsKind.XmlComment) != 0 )
        {
            iS += 3;
            for(; ; )
            {
                if( ++iS == s.Length ) return (int)TokenType.ErrorClosingXmlComment;
                if( s.StartsWith( "-->" ) )
                {
                    collector.Add( new Trivia( TokenType.XmlComment, head.Slice( iCurrent, iS ) ) );
                    if( ++iS == s.Length ) return head.Length;
                    if( s[iS] == '/' )
                    {
                        iCurrent += iS;
                        s = s.Slice( iS );
                        break;
                    }
                }
            }
        }
        if( s.StartsWith( "--" ) && (type & AllowedCommentsKind.SqlComment) != 0 )
        {
            iS += 1;
            while( ++iS < s.Length && s[iS] != '\n' ) ;
            if( iS < s.Length && s[iS] == '\r' ) ++iS;
            collector.Add( new Trivia( TokenType.SqlComment, head.Slice( iCurrent, iS ) ) );
            iCurrent += iS;
            // An end of line ends a trailing trivia.
            if( !isLeading ) return iCurrent;
            s = s.Slice( iS );
            goto again;
        }
        return iCurrent;
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
