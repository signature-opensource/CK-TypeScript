using CK.Core;
using System;

namespace CK.Transform.Core;

/// <summary>
/// Extends <see cref="BasicTokenType"/> and <see cref="TokenType"/>.
/// </summary>
public static class TokenTypeExtensions
{
    static string?[] _classNames;

    static ReadOnlySpan<byte> _basicTokenType => [
        /* 0*/ 0,0,0,0,0,0,0,0,
        /* 1*/ 0,0,0,0,0,0,0,0,
        /* 2*/ 0,0,0,0,0,0,0,0,
        /* 3*/ 0,0,0,0,0,0,0,0,
        /* 4*/ 0,(byte)TokenType.Exclamation,(byte)TokenType.DoubleQuote,(byte)TokenType.Hash,(byte)TokenType.Dollar,(byte)TokenType.Percent,(byte)TokenType.Ampersand,(byte)TokenType.SingleQuote,
        /* 5*/ (byte)TokenType.OpenParen,(byte)TokenType.CloseParen,(byte)TokenType.Asterisk,(byte)TokenType.Plus,(byte)TokenType.Comma,(byte)TokenType.Minus,(byte)TokenType.Dot,(byte)TokenType.Slash,
        /* 6*/ 0,0,0,0,0,0,0,0,
        /* 7*/ 0,0,(byte)TokenType.Colon,(byte)TokenType.SemiColon,(byte)TokenType.LessThan,(byte)TokenType.Equals,(byte)TokenType.GreaterThan,(byte)TokenType.Question,
        /* 8*/ (byte)TokenType.AtSign,0,0,0,0,0,0,0,
        /* 9*/ 0,0,0,0,0,0,0,0,
        /*10*/ 0,0,0,0,0,0,0,0,
        /*11*/ 0,0,0,(byte)TokenType.OpenBracket,(byte)TokenType.BackSlash,(byte)TokenType.CloseBracket,(byte)TokenType.Caret,(byte)TokenType.Underscore,
        /*12*/ (byte)TokenType.BackTick,0,0,0,0,0,0,0,
        /*13*/ 0,0,0,0,0,0,0,0,
        /*14*/ 0,0,0,0,0,0,0,0,
        /*15*/ 0,0,0,(byte)TokenType.OpenBrace,(byte)TokenType.Bar,(byte)TokenType.CloseBrace,(byte)TokenType.Tilde,0,
        ];

    static TokenTypeExtensions()
    {
        _classNames = new string?[(int)TokenType.MaxClassNumber + 1];
        _classNames[0] = "Error";
        _classNames[(int)TokenType.TriviaClassNumber] = "Trivia";

        Throw.DebugAssert( _basicTokenType['['] == (byte)TokenType.OpenBracket );
        Throw.DebugAssert( _basicTokenType[']'] == (byte)TokenType.CloseBracket );
        Throw.DebugAssert( _basicTokenType['('] == (byte)TokenType.OpenParen );
        Throw.DebugAssert( _basicTokenType[')'] == (byte)TokenType.CloseParen );
        Throw.DebugAssert( _basicTokenType['{'] == (byte)TokenType.OpenBrace );
        Throw.DebugAssert( _basicTokenType['}'] == (byte)TokenType.CloseBrace );
        Throw.DebugAssert( _basicTokenType['<'] == (byte)TokenType.LessThan );
        Throw.DebugAssert( _basicTokenType['>'] == (byte)TokenType.GreaterThan );

        Throw.DebugAssert( _basicTokenType['&'] == (byte)TokenType.Ampersand );
        Throw.DebugAssert( _basicTokenType['*'] == (byte)TokenType.Asterisk );
        Throw.DebugAssert( _basicTokenType['@'] == (byte)TokenType.AtSign );
        Throw.DebugAssert( _basicTokenType['\\'] == (byte)TokenType.BackSlash );
        Throw.DebugAssert( _basicTokenType['`'] == (byte)TokenType.BackTick );
        Throw.DebugAssert( _basicTokenType['|'] == (byte)TokenType.Bar );
        Throw.DebugAssert( _basicTokenType['^'] == (byte)TokenType.Caret );
        Throw.DebugAssert( _basicTokenType[':'] == (byte)TokenType.Colon );
        Throw.DebugAssert( _basicTokenType[','] == (byte)TokenType.Comma );
        Throw.DebugAssert( _basicTokenType['$'] == (byte)TokenType.Dollar );
        Throw.DebugAssert( _basicTokenType['.'] == (byte)TokenType.Dot );
        Throw.DebugAssert( _basicTokenType['\"'] == (byte)TokenType.DoubleQuote );
        Throw.DebugAssert( _basicTokenType['='] == (byte)TokenType.Equals );
        Throw.DebugAssert( _basicTokenType['!'] == (byte)TokenType.Exclamation);
        Throw.DebugAssert( _basicTokenType['#'] == (byte)TokenType.Hash );
        Throw.DebugAssert( _basicTokenType['-'] == (byte)TokenType.Minus );
        Throw.DebugAssert( _basicTokenType['%'] == (byte)TokenType.Percent );
        Throw.DebugAssert( _basicTokenType['+'] == (byte)TokenType.Plus );
        Throw.DebugAssert( _basicTokenType['?'] == (byte)TokenType.Question );
        Throw.DebugAssert( _basicTokenType[';'] == (byte)TokenType.SemiColon );
        Throw.DebugAssert( _basicTokenType['\''] == (byte)TokenType.SingleQuote );
        Throw.DebugAssert( _basicTokenType['/'] == (byte)TokenType.Slash );
        Throw.DebugAssert( _basicTokenType['~'] == (byte)TokenType.Tilde );
        Throw.DebugAssert( _basicTokenType['_'] == (byte)TokenType.Underscore );
    }

    /// <summary>
    /// Reserves a token class.
    /// </summary>
    /// <param name="classNumber">The number to reserve.</param>
    /// <param name="tokenClassName">The token class name.</param>
    /// <returns>The class number.</returns>
    public static int ReserveTokenClass( int classNumber, string tokenClassName )
    {
        Throw.CheckOutOfRangeArgument( classNumber >= 0 && classNumber <= (int)TokenType.MaxClassNumber );
        Throw.CheckNotNullOrWhiteSpaceArgument( tokenClassName );
        lock( _classNames )
        {
            var already = _classNames[classNumber];
            if( already != null )
            {
                Throw.InvalidOperationException( $"The class '{tokenClassName}' cannot use n°{classNumber}, this number is already reserved by '{already}'." );
            }
            _classNames[classNumber] = tokenClassName;
        }
        return classNumber;
    }

    /// <summary>
    /// Gets whether this is <see cref="TokenType.None"/> or <see cref="TokenType.ErrorClassBit"/> is set.
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is None or an error.</returns>
    public static bool IsErrorOrNone( this TokenType type ) => type <= 0;

    /// <summary>
    /// Gets whether <see cref="TokenType.ErrorClassBit"/> is set.
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token an error.</returns>
    public static bool IsError( this TokenType type ) => type < 0;

    /// <summary>
    /// Gets whether this token type is a <see cref="Trivia"/>, including
    /// <see cref="TokenType.Whitespace"/> and possibly on error (<see cref="TokenType.ClassMaskAllowError"/> is used).
    /// <para>
    /// Only Trivia (not <see cref="Token"/>) can carry a trivia token type.
    /// </para>
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a trivia.</returns>
    public static bool IsTrivia( this TokenType type ) => (type & TokenType.ClassMaskAllowError) == TokenType.TriviaClassBit;

    /// <summary>
    /// Gets whether this is a whitespace trivia.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsTriviaWhitespace( this TokenType type ) => type == TokenType.Whitespace;

    /// <summary>
    /// Gets whether this is a comment trivia.
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a line comment.</returns>
    public static bool IsTriviaComment( this TokenType type ) => (type & TokenType.ClassMaskAllowError) == TokenType.TriviaClassMask
                                                                && (type & TokenType.TriviaCommentMask) != 0;

    /// <summary>
    /// Gets whether this is a Line comment trivia.
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a line comment.</returns>
    public static bool IsTriviaLineComment( this TokenType type ) => (type & TokenType.ClassMaskAllowError) == TokenType.TriviaClassBit
                                                                    && (type & TokenType.TriviaCommentStartLengthMask) != 0
                                                                    && (type & TokenType.TriviaCommentEndLengthMask) == 0;

    /// <summary>
    /// Gets whether this is a Block comment trivia.
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a line comment.</returns>
    public static bool IsTriviaBlockComment( this TokenType type ) => (type & TokenType.ClassMaskAllowError) == TokenType.TriviaClassBit
                                                                     && (type & TokenType.TriviaCommentEndLengthMask) != 0;
    /// <summary>
    /// Computes a type for a line comment with a given start delimiter length.
    /// </summary>
    /// <param name="commentStartLength">The starting delimiter length. Must be greater than 0 and lower than 16.</param>
    /// <returns>The line comment type.</returns>
    public static TokenType GetTriviaLineCommentType( int commentStartLength )
    {
        Throw.CheckOutOfRangeArgument( commentStartLength >= 1 && commentStartLength < 16 );
        return TokenType.TriviaClassBit | (TokenType)commentStartLength;
    }

    /// <summary>
    /// Computes a type for a block comment with a given start and end delimiter lengths.
    /// </summary>
    /// <param name="commentStartLength">The starting delimiter length. Must be greater than 0 and lower than 16.</param>
    /// <param name="commentEndLength">The starting delimiter length. Must be greater than 0 and lower than 8.</param>
    /// <returns>The block comment type.</returns>
    public static TokenType GetTriviaBlockCommentType( int commentStartLength, int commentEndLength )
    {
        Throw.CheckOutOfRangeArgument( commentEndLength >= 1 && commentEndLength < 8 );
        return GetTriviaLineCommentType( commentStartLength ) | (TokenType)(commentEndLength << 4);
    }

    /// <summary>
    /// Gets the comment start length.
    /// </summary>
    /// <param name="type">this token type.</param>
    /// <returns>The number of characters of the starting delimiter. 0 if this is not a comment trivia type.</returns>
    public static int GetTriviaCommentStartLength( this TokenType type ) => IsTrivia( type ) ? ((int)type & 15) : 0;

    /// <summary>
    /// Gets the comment end length.
    /// </summary>
    /// <param name="type">this token type.</param>
    /// <returns>The number of characters of the ending delimiter. 0 if this is not a block comment trivia type.</returns>
    public static int GetTriviaCommentEndLength( this TokenType type ) => IsTrivia( type ) ? (((int)type >> 4) & 7) : 0;

    /// <summary>
    /// Gets a one char known <see cref="TokenType"/> or <see cref="TokenType.None"/> if it is not defined.
    /// This is the starting point of the <see cref="LowLevelToken.GetBasicTokenType(ReadOnlySpan{char})"/>.
    /// </summary>
    /// <param name="c">The character.</param>
    /// <returns>A known one character node type.</returns>
    public static TokenType GetSingleCharType( char c )
    {
        return (uint)c < 256 ? (TokenType)_basicTokenType[(int)c] : TokenType.None;
    }
}

