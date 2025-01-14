using CK.Core;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;

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
    readonly TokenType _tokenType;

    /// <summary>
    /// Gets a single whitespace as trivias.
    /// </summary>
    public static ImmutableArray<Trivia> OneSpace => TriviaExtensions.OneSpace;

    /// <summary>
    /// Gets the <see cref="Environment.NewLine"/> as trivias.
    /// </summary>
    public static ImmutableArray<Trivia> NewLine => TriviaExtensions.NewLine;

    /// <summary>
    /// Gets empty trivias.
    /// </summary>
    public static ImmutableArray<Trivia> Empty => ImmutableArray<Trivia>.Empty;

    /// <summary>
    /// Initializes a new Trivia. The <paramref name="content"/> is checked by default:
    /// it must be compatible with the <paramref name="tokenType"/>:
    /// <list type="bullet">
    ///     <item>For a block comment, its length must be greater than the <see cref="CommentStartLength"/> + <see cref="CommentEndLength"/>.</item>
    ///     <item>For a line comment, it must at least contain <see cref="CommentStartLength"/> characters and ends with a <see cref="Environment.NewLine"/>.</item>
    /// </list>
    /// Checks are always skipped if the token type is on error.
    /// Note that <see cref="TokenType.Whitespace"/> are free to contain other characters than white spaces. it is considered as a whitespace but
    /// may contain actual characters.
    /// </summary>
    /// <param name="tokenType">The token type. Must be a Trivia (may be on error).</param>
    /// <param name="content">The raw content.</param>
    /// <param name="checkContent">False to skip the check of the content.</param>
    public Trivia( TokenType tokenType, string content, bool checkContent = true )
        : this( tokenType, content.AsMemory(), checkContent )
    {
    }

    /// <summary>
    /// Initializes a new Trivia. The <paramref name="content"/> is NOT checked by default:
    /// it must be compatible with the <paramref name="tokenType"/>:
    /// <list type="bullet">
    ///     <item>For a block comment, its length must be greater than the <see cref="CommentStartLength"/> + <see cref="CommentEndLength"/>.</item>
    ///     <item>For a line comment, it must at least contain <see cref="CommentStartLength"/> characters and ends with a <see cref="Environment.NewLine"/>.</item>
    /// </list>
    /// Checks are always skipped if the token type is on error.
    /// Note that <see cref="TokenType.Whitespace"/> are free to contain other characters than white spaces. it is considered as a whitespace but
    /// may contain actual characters.
    /// </summary>
    /// <param name="tokenType">The token type. Must be a Trivia (may be on error).</param>
    /// <param name="content">The raw content.</param>
    /// <param name="checkContent">True to check the content.</param>
    public Trivia( TokenType tokenType, ReadOnlyMemory<char> content, bool checkContent = false )
    {
        Throw.CheckArgument( tokenType.IsTrivia() );
        Throw.CheckArgument( content.Length > 0 );
        _tokenType = tokenType;
        _content = content;
        if( checkContent ) DoCheckContent();
    }

    void DoCheckContent()
    {
        if( !IsError && !IsWhitespace )
        {
            if( IsLineComment )
            {
                Throw.CheckArgument( Content.Span.EndsWith( Environment.NewLine ) );
                Throw.CheckArgument( Content.Length >= CommentStartLength + Environment.NewLine.Length );
                return;
            }
            Throw.DebugAssert( IsBlockComment );
            Throw.CheckArgument( Content.Length > CommentStartLength + CommentEndLength );
        }
    }

    /// <summary>
    /// Gets the token type that necessarily belongs to <see cref="TokenType.TriviaClassBit"/>
    /// or is <see cref="TokenType.None"/> if <see cref="IsDefault"/> is true.
    /// </summary>
    public TokenType TokenType => _tokenType;

    /// <summary>
    /// Gets whether this trivia is an error.
    /// (The <see cref="TokenType.ErrorClassBit"/> is set in the <see cref="TokenType"/>.)
    /// </summary>
    public bool IsError => _tokenType < 0;

    /// <summary>
    /// Gets whether this trivia is an invalid <c>default</c>.
    /// </summary>
    public bool IsDefault => _tokenType == TokenType.None;

    /// <summary>
    /// Gets the content of this trivia including content delimiters like the "//" comment start characters.
    /// </summary>
    public ReadOnlyMemory<char> Content => _content;

    /// <summary>
    /// Gets wether this is a whitespace trivia.
    /// <para>If this is not a whitespace trivia, then it is a comment.</para>
    /// </summary>
    public bool IsWhitespace => _tokenType.IsTriviaWhitespace();

    /// <summary>
    /// Gets wether this is a line comment.
    /// </summary>
    public bool IsLineComment
    {
        get
        {
            Throw.DebugAssert( _tokenType.IsTriviaLineComment() == ((_tokenType & TokenType.TriviaCommentStartLengthMask) != 0 && (_tokenType & TokenType.TriviaCommentEndLengthMask) == 0) );
            return (_tokenType & TokenType.TriviaCommentStartLengthMask) != 0 && (_tokenType & TokenType.TriviaCommentEndLengthMask) == 0;
        }
    }

    /// <summary>
    /// Gets wether this is a block comment.
    /// </summary>
    public bool IsBlockComment
    {
        get
        {
            Throw.DebugAssert( _tokenType.IsTriviaBlockComment() == ((_tokenType & TokenType.TriviaCommentEndLengthMask) != 0) );
            return (_tokenType & TokenType.TriviaCommentEndLengthMask) != 0;
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
            Throw.DebugAssert( _tokenType.GetTriviaCommentStartLength() == ((int)_tokenType & 15) );
            return ((int)_tokenType & 15);
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
            Throw.DebugAssert( _tokenType.GetTriviaCommentEndLength() == (((int)_tokenType >> 4) & 7) );
            return ((int)_tokenType >> 4) & 7;
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
}
