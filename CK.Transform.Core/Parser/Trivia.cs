using CK.Core;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// A trivia is consecutive whitespaces (including newlines) or a comment.
/// <para>
/// Comments can be single line (ends with a new line like the C-like //) or multiple lines (like the C-like /* block */).
/// The <see cref="Content"/> covers the whole trivia (including comment start and end pattern and final new line for line comments).
/// </para>
/// <para>
/// The <see cref="TokenType"/> doesn't encode the kind of trivia (like a "CLikeStarComment" for "/* ... */" comments). Instead,
/// a bit indicates whether it is a Line vs. Block comment and the length of the start and end patterns are encoded on 3 bits (opening
/// and closing pattern can be from 1 to 7 characters).
/// </para>
/// </summary>
public readonly struct Trivia : IEquatable<Trivia>
{
    readonly ReadOnlyMemory<char> _content;
    readonly NodeType _tokenType;

    public static ImmutableArray<Trivia> OneSpace => Analyzer.OneSpace;

    public Trivia( NodeType tokenType, string content )
        : this( tokenType, content.AsMemory() )
    {
    }

    public Trivia( NodeType tokenType, ReadOnlyMemory<char> content )
    {
        Throw.CheckArgument( tokenType.IsTrivia() );
        Throw.CheckArgument( content.Length > 0 );
        _tokenType = tokenType;
        _content = content;
    }

    /// <summary>
    /// Gets the token type that necessarily belongs to <see cref="NodeType.TriviaClassBit"/>
    /// or is <see cref="NodeType.None"/> if <see cref="IsValid"/> is false.
    /// </summary>
    public NodeType TokenType => _tokenType;

    /// <summary>
    /// Gets whether this trivia is valid.
    /// </summary>
    public bool IsValid => _tokenType != NodeType.None;

    /// <summary>
    /// Gets the content of this trivia including content delimiters like the "//" comment start characters.
    /// </summary>
    public ReadOnlyMemory<char> Content => _content;

    /// <summary>
    /// Gets wether this is a whitespace trivia.
    /// <para>If this is not a whitespace trivia, then it is a comment.</para>
    /// </summary>
    public bool IsWhitespace => _tokenType.IsWhitespace();

    /// <summary>
    /// Gets wether this is a line comment.
    /// </summary>
    public bool IsLineComment
    {
        get
        {
            Throw.DebugAssert( _tokenType.IsTriviaLineComment() == ((_tokenType & NodeType.TriviaCommentStartLengthMask) != 0 && (_tokenType & NodeType.TriviaCommentEndLengthMask) == 0) );
            return (_tokenType & NodeType.TriviaCommentStartLengthMask) != 0 && (_tokenType & NodeType.TriviaCommentEndLengthMask) == 0;
        }
    }

    /// <summary>
    /// Gets wether this is a block comment.
    /// </summary>
    public bool IsBlockComment
    {
        get
        {
            Throw.DebugAssert( _tokenType.IsTriviaBlockComment() == ((_tokenType & NodeType.TriviaCommentEndLengthMask) != 0) );
            return (_tokenType & NodeType.TriviaCommentEndLengthMask) != 0;
        }
    }

    /// <summary>
    /// Gets the length of the comment start.
    /// Defaults to 0 if this is not a comment.
    /// </summary>
    public int CommentStartLength
    {
        get
        {
            Throw.DebugAssert( _tokenType.GetTriviaCommentStartLength() == ((int)_tokenType & 3) );
            return ((int)_tokenType & 3);
        }
    }

    /// <summary>
    /// Gets the length of the comment end.
    /// Defaults to 0 if this is not a block comment.
    /// </summary>
    public int CommentEndLength
    {
        get
        {
            Throw.DebugAssert( _tokenType.GetTriviaCommentEndLength() == (((int)_tokenType >> 3) & 3) );
            return ((int)_tokenType >> 3) & 3;
        }
    }


    /// <summary>
    /// Overridden to return the <see cref="Content"/>.
    /// </summary>
    /// <returns>The text.</returns>
    public override string ToString() => _content.ToString();

    public bool Equals( Trivia other ) => _tokenType == other._tokenType && _content.Span.SequenceEqual( other._content.Span );

    public override int GetHashCode() => HashCode.Combine( _tokenType, _tokenType );

    public override bool Equals( object? o ) => o is Trivia other && Equals( other );

    public static bool operator ==( Trivia left, Trivia right ) => left.Equals( right );

    public static bool operator !=( Trivia left, Trivia right ) => !(left == right);

    static public void ToMiddle<TL, TM, TR>( ref TL left, ref TM middle, ref TR right )
        where TL : class, IAbstractNode
        where TM : class, IAbstractNode
        where TR : class, IAbstractNode
    {
        ToRight( ref left, ref middle );
        ToLeft( ref middle, ref right );
    }

    static public void ToLeft<TL, TR>( ref TL left, ref TR right )
        where TL : class, IAbstractNode
        where TR : class, IAbstractNode
    {
        var transfer = right.LeadingTrivias;
        right = right.SetTrivias( ImmutableArray<Trivia>.Empty, right.TrailingTrivias );
        left = left.SetTrivias( left.LeadingTrivias, left.TrailingTrivias.AddRange( transfer ) );
    }

    static public void ToRight<TL, TR>( ref TL left, ref TR right )
        where TL : class, IAbstractNode
        where TR : class, IAbstractNode
    {
        var transfer = left.TrailingTrivias;
        left = left.SetTrivias( left.LeadingTrivias, ImmutableArray<Trivia>.Empty );
        right = right.SetTrivias( transfer.AddRange( right.LeadingTrivias ), right.TrailingTrivias );
    }

    static public void WhiteSpaceToMiddle<TL, TM, TR>( ref TL left, ref TM middle, ref TR right )
        where TL : class, IAbstractNode
        where TM : class, IAbstractNode
        where TR : class, IAbstractNode
    {
        WhiteSpaceToRight( ref left, ref middle );
        WhiteSpaceToLeft( ref middle, ref right );
    }

    static public Tuple<TL, TM, TR> WhiteSpaceToMiddle<TL, TM, TR>( TL left, TM middle, TR right )
         where TL : class, IAbstractNode
         where TM : class, IAbstractNode
         where TR : class, IAbstractNode
    {
        WhiteSpaceToRight( ref left, ref middle );
        WhiteSpaceToLeft( ref middle, ref right );
        return Tuple.Create( left, middle, right );
    }

    static public void WhiteSpaceToRight<TL, TR>( ref TL left, ref TR right )
        where TL : class, IAbstractNode
        where TR : class, IAbstractNode
    {
        IAbstractNode r = right;
        left = left.ExtractTrailingTrivias( ( t, idx ) =>
        {
            if( t.TokenType == NodeType.Whitespace )
            {
                r = r.AddLeadingTrivia( t );
                return true;
            }
            return false;
        } );
        right = (TR)r;
    }

    static public Tuple<TL, TR> WhiteSpaceToRight<TL, TR>( TL left, TR right )
         where TL : class, IAbstractNode
         where TR : class, IAbstractNode
    {
        WhiteSpaceToRight( ref left, ref right );
        return Tuple.Create( left, right );
    }

    static public void WhiteSpaceToLeft<TL, TR>( ref TL left, ref TR right )
        where TL : class, IAbstractNode
        where TR : class, IAbstractNode
    {
        IAbstractNode l = left;
        right = right.ExtractLeadingTrivias( ( t, idx ) =>
        {
            if( t.TokenType == NodeType.Whitespace )
            {
                l = l.AddTrailingTrivia( t );
                return true;
            }
            return false;
        } );
        left = (TL)l;
    }

    static public Tuple<TL, TR> WhiteSpaceToLeft<TL, TR>( TL left, TR right )
         where TL : class, IAbstractNode
         where TR : class, IAbstractNode
    {
        WhiteSpaceToLeft( ref left, ref right );
        return Tuple.Create( left, right );
    }
}
