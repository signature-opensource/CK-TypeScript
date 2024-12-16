using CK.Core;
using System;

namespace CK.Transform.Core;

public static class NodeTypeExtensions
{
    static string?[] _classNames;

    static ReadOnlySpan<byte> _basicNodeType => [
        /* 0*/ 0,0,0,0,0,0,0,0,
        /* 1*/ 0,0,0,0,0,0,0,0,
        /* 2*/ 0,0,0,0,0,0,0,0,
        /* 3*/ 0,0,0,0,0,0,0,0,
        /* 4*/ 0,(byte)NodeType.Exclamation,(byte)NodeType.DoubleQuote,(byte)NodeType.Hash,(byte)NodeType.Dollar,(byte)NodeType.Percent,(byte)NodeType.Ampersand,(byte)NodeType.SingleQuote,
        /* 5*/ (byte)NodeType.OpenParen,(byte)NodeType.CloseParen,(byte)NodeType.Asterisk,(byte)NodeType.Plus,0,(byte)NodeType.Minus,(byte)NodeType.Dot,(byte)NodeType.Slash,
        /* 6*/ 0,0,0,0,0,0,0,0,
        /* 7*/ 0,0,0,0,(byte)NodeType.LessThan,(byte)NodeType.Equals,(byte)NodeType.GreaterThan,(byte)NodeType.Question,
        /* 8*/ (byte)NodeType.AtSign,0,0,0,0,0,0,0,
        /* 9*/ 0,0,0,0,0,0,0,0,
        /*10*/ 0,0,0,0,0,0,0,0,
        /*11*/ 0,0,0,(byte)NodeType.OpenBracket,(byte)NodeType.BackSlash,(byte)NodeType.CloseBracket,(byte)NodeType.Caret,0,
        /*12*/ (byte)NodeType.BackTick,0,0,0,0,0,0,0,
        /*13*/ 0,0,0,0,0,0,0,0,
        /*14*/ 0,0,0,0,0,0,0,0,
        /*15*/ 0,0,0,0,(byte)NodeType.Bar,0,(byte)NodeType.Tilde,0,
        ];

    static NodeTypeExtensions()
    {
        _classNames = new string?[(int)NodeType.MaxClassNumber + 1];
        _classNames[0] = "Error";
        _classNames[(int)NodeType.TriviaClassNumber] = "Trivia";

        Throw.DebugAssert( _basicNodeType['['] == (byte)NodeType.OpenBracket );
        Throw.DebugAssert( _basicNodeType[']'] == (byte)NodeType.CloseBracket );
        Throw.DebugAssert( _basicNodeType['('] == (byte)NodeType.OpenParen );
        Throw.DebugAssert( _basicNodeType[')'] == (byte)NodeType.CloseParen );
        Throw.DebugAssert( _basicNodeType['<'] == (byte)NodeType.LessThan );
        Throw.DebugAssert( _basicNodeType['>'] == (byte)NodeType.GreaterThan );

        Throw.DebugAssert( _basicNodeType['='] == (byte)NodeType.Equals );
        Throw.DebugAssert( _basicNodeType['\"'] == (byte)NodeType.DoubleQuote );
        Throw.DebugAssert( _basicNodeType['\''] == (byte)NodeType.SingleQuote );
        Throw.DebugAssert( _basicNodeType['`'] == (byte)NodeType.BackTick );
        Throw.DebugAssert( _basicNodeType['.'] == (byte)NodeType.Dot );
        Throw.DebugAssert( _basicNodeType['?'] == (byte)NodeType.Question );
        Throw.DebugAssert( _basicNodeType['|'] == (byte)NodeType.Bar );
        Throw.DebugAssert( _basicNodeType['^'] == (byte)NodeType.Caret );
        Throw.DebugAssert( _basicNodeType['+'] == (byte)NodeType.Plus );
        Throw.DebugAssert( _basicNodeType['-'] == (byte)NodeType.Minus );
        Throw.DebugAssert( _basicNodeType['*'] == (byte)NodeType.Asterisk );
        Throw.DebugAssert( _basicNodeType['/'] == (byte)NodeType.Slash );
        Throw.DebugAssert( _basicNodeType['\\'] == (byte)NodeType.BackSlash );
        Throw.DebugAssert( _basicNodeType['%'] == (byte)NodeType.Percent );
        Throw.DebugAssert( _basicNodeType['$'] == (byte)NodeType.Dollar );
        Throw.DebugAssert( _basicNodeType['@'] == (byte)NodeType.AtSign );
        Throw.DebugAssert( _basicNodeType['#'] == (byte)NodeType.Hash );
        Throw.DebugAssert( _basicNodeType['&'] == (byte)NodeType.Ampersand );
        Throw.DebugAssert( _basicNodeType['~'] == (byte)NodeType.Tilde );
        Throw.DebugAssert( _basicNodeType['!'] == (byte)NodeType.Exclamation);
    }

    /// <summary>
    /// Reserves a token class.
    /// </summary>
    /// <param name="classNumber">The number to reserve.</param>
    /// <param name="tokenClassName">The token class name.</param>
    public static void ReserveTokenClass( int classNumber, string tokenClassName )
    {
        Throw.CheckOutOfRangeArgument( classNumber >= 0 && classNumber <= (int)NodeType.MaxClassNumber );
        Throw.CheckNotNullOrWhiteSpaceArgument( tokenClassName );
        var already = _classNames[classNumber];
        if( already != null )
        {
            Throw.InvalidOperationException( $"The class '{tokenClassName}' cannot use n°{classNumber}, this number is already reserved by '{already}'." );
        }
        _classNames[classNumber] = tokenClassName;
    }


    /// <summary>
    /// Gets whether this is <see cref="NodeType.None"/> or <see cref="NodeType.ErrorClassBit"/> is set.
    /// <para>
    /// Only <see cref="TokenErrorNode"/> can carry an error token type.
    /// </para>
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is an error.</returns>
    public static bool IsError( this NodeType type ) => type <= 0;

    /// <summary>
    /// Gets whether this token type is a <see cref="Trivia"/>, including
    /// <see cref="NodeType.Whitespace"/>.
    /// <para>
    /// Only Trivia (not <see cref="TokenNode"/>) can carry a trivia token type.
    /// </para>
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a trivia.</returns>
    public static bool IsTrivia( this NodeType type ) => (type & NodeType.TriviaClassMask) == NodeType.TriviaClassBit;

    /// <summary>
    /// Gets whether this is a whitespace trivia.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsWhitespace( this NodeType type ) => type == NodeType.Whitespace;

    /// <summary>
    /// Gets whether this is a comment trivia.
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a line comment.</returns>
    public static bool IsTriviaComment( this NodeType type ) => (type & NodeType.TriviaClassMask) == NodeType.TriviaClassMask
                                                                && (type & NodeType.TriviaCommentMask) != 0;

    /// <summary>
    /// Gets whether this is a Line comment trivia.
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a line comment.</returns>
    public static bool IsTriviaLineComment( this NodeType type ) => (type & NodeType.TriviaClassMask) == NodeType.TriviaClassMask
                                                                    && (type & NodeType.TriviaCommentStartLengthMask) != 0
                                                                    && (type & NodeType.TriviaCommentEndLengthMask) == 0;

    /// <summary>
    /// Gets whether this is a Block comment trivia.
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a line comment.</returns>
    public static bool IsTriviaBlockComment( this NodeType type ) => (type & NodeType.TriviaClassMask) == NodeType.TriviaClassMask
                                                                     && (type & NodeType.TriviaCommentEndLengthMask) != 0;
    /// <summary>
    /// Computes a type for a line comment with a given start delimiter length.
    /// </summary>
    /// <param name="commentStartLength">The starting delimiter length. Must be greater than 0 and lower than 8.</param>
    /// <returns>The line comment type.</returns>
    public static NodeType GetTriviaLineCommentType( int commentStartLength )
    {
        Throw.CheckOutOfRangeArgument( commentStartLength >= 1 && commentStartLength < 8 );
        return NodeType.TriviaClassBit | (NodeType)commentStartLength;
    }

    /// <summary>
    /// Computes a type for a block comment with a given start and end delimiter lengths.
    /// </summary>
    /// <param name="commentStartLength">The starting delimiter length. Must be greater than 0 and lower than 8.</param>
    /// <param name="commentEndLength">The starting delimiter length. Must be greater than 0 and lower than 8.</param>
    /// <returns>The block comment type.</returns>
    public static NodeType GetTriviaBlockCommentType( int commentStartLength, int commentEndLength )
    {
        Throw.CheckOutOfRangeArgument( commentEndLength >= 1 && commentEndLength < 8 );
        return GetTriviaLineCommentType( commentStartLength ) | (NodeType)(commentEndLength << 3);
    }

    internal static int GetTriviaCommentStartLength( this NodeType type ) => IsTrivia( type ) ? ((int)type & 3) : 0;

    internal static int GetTriviaCommentEndLength( this NodeType type ) => IsTrivia( type ) ? (((int)type >> 3) & 3) : 0;

    /// <summary>
    /// Gets a one char known <see cref="NodeType"/> or <see cref="NodeType.None"/> if it is not defined.
    /// This is the starting point of the <see cref="LowLevelToken.GetBasicTokenType(ReadOnlySpan{char})"/>.
    /// </summary>
    /// <param name="c">The character.</param>
    /// <returns>A known one character node type.</returns>
    public static NodeType GetSingleCharType( char c )
    {
        return (uint)c < 256 ? (NodeType)_basicNodeType[(int)c] : NodeType.None;
    }
}

