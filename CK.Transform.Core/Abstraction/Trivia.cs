using CK.Core;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml;

namespace CK.Transform.Core;

/// <summary>
/// A trivia is a an unparsed text or a comment.
/// </summary>
public readonly struct Trivia : IEquatable<Trivia>
{
    readonly TokenType _tokenType;
    readonly ReadOnlyMemory<char> _content;

    /// <summary>
    /// A single space trivias.
    /// </summary>
    public static readonly ImmutableArray<Trivia> OneSpace = [new Trivia( TokenType.Whitespace, " " )];

    public Trivia( TokenType tokenType, string content )
        : this( tokenType, content.AsMemory() )
    {
    }

    public Trivia( TokenType tokenType, ReadOnlyMemory<char> content )
    {
        Throw.CheckArgument( tokenType.IsTrivia() );
        Throw.CheckArgument( content.Length > 0 );
        _tokenType = tokenType;
        _content = content;
    }

    /// <summary>
    /// Gets the token type that necessarily belongs to <see cref="TokenType.TriviaClassBit"/>
    /// or is <see cref="TokenType.None"/> if <see cref="IsValid"/> is false.
    /// </summary>
    public TokenType TokenType => _tokenType;

    /// <summary>
    /// Gets whether this trivia is valid.
    /// </summary>
    public bool IsValid => _tokenType != TokenType.None;

    /// <summary>
    /// Gets the content of this trivia including delimiters like the prefix "//" characters
    /// for <see cref="TokenType.LineComment"/>.
    /// </summary>
    public ReadOnlyMemory<char> Content => _content;

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
        where TL : AbstractNode
        where TM : AbstractNode
        where TR : AbstractNode
    {
        ToRight( ref left, ref middle );
        ToLeft( ref middle, ref right );
    }

    static public void ToLeft<TL, TR>( ref TL left, ref TR right )
        where TL : AbstractNode
        where TR : AbstractNode
    {
        var transfer = right.LeadingTrivias;
        right = right.SetTrivias( ImmutableArray<Trivia>.Empty, right.TrailingTrivias );
        left = left.SetTrivias( left.LeadingTrivias, left.TrailingTrivias.AddRange( transfer ) );
    }

    static public void ToRight<TL, TR>( ref TL left, ref TR right )
        where TL : AbstractNode
        where TR : AbstractNode
    {
        var transfer = left.TrailingTrivias;
        left = left.SetTrivias( left.LeadingTrivias, ImmutableArray<Trivia>.Empty );
        right = right.SetTrivias( transfer.AddRange( right.LeadingTrivias ), right.TrailingTrivias );
    }

    static public void WhiteSpaceToMiddle<TL, TM, TR>( ref TL left, ref TM middle, ref TR right )
        where TL : AbstractNode
        where TM : AbstractNode
        where TR : AbstractNode
    {
        WhiteSpaceToRight( ref left, ref middle );
        WhiteSpaceToLeft( ref middle, ref right );
    }

    static public Tuple<TL, TM, TR> WhiteSpaceToMiddle<TL, TM, TR>( TL left, TM middle, TR right )
         where TL : AbstractNode
         where TM : AbstractNode
         where TR : AbstractNode
    {
        WhiteSpaceToRight( ref left, ref middle );
        WhiteSpaceToLeft( ref middle, ref right );
        return Tuple.Create( left, middle, right );
    }

    static public void WhiteSpaceToRight<TL, TR>( ref TL left, ref TR right )
        where TL : AbstractNode
        where TR : AbstractNode
    {
        AbstractNode r = right;
        left = left.ExtractTrailingTrivias( ( t, idx ) =>
        {
            if( t.TokenType == TokenType.Whitespace )
            {
                r = r.AddLeadingTrivia( t );
                return true;
            }
            return false;
        } );
        right = (TR)r;
    }

    static public Tuple<TL, TR> WhiteSpaceToRight<TL, TR>( TL left, TR right )
         where TL : AbstractNode
         where TR : AbstractNode
    {
        WhiteSpaceToRight( ref left, ref right );
        return Tuple.Create( left, right );
    }

    static public void WhiteSpaceToLeft<TL, TR>( ref TL left, ref TR right )
        where TL : AbstractNode
        where TR : AbstractNode
    {
        AbstractNode l = left;
        right = right.ExtractLeadingTrivias( ( t, idx ) =>
        {
            if( t.TokenType == TokenType.Whitespace )
            {
                l = l.AddTrailingTrivia( t );
                return true;
            }
            return false;
        } );
        left = (TL)l;
    }

    static public Tuple<TL, TR> WhiteSpaceToLeft<TL, TR>( TL left, TR right )
         where TL : AbstractNode
         where TR : AbstractNode
    {
        WhiteSpaceToLeft( ref left, ref right );
        return Tuple.Create( left, right );
    }
}