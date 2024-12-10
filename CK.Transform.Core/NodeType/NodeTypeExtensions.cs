using CK.Core;

namespace CK.Transform.Core;

public static class NodeTypeExtensions
{
    static string?[] _classNames;

    static NodeTypeExtensions()
    {
        _classNames = new string?[(int)NodeType.MaxClassNumber + 1];
        _classNames[0] = "Error";
        _classNames[(int)NodeType.TriviaClassNumber] = "Trivia";
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

    public static NodeType GetTriviaLineCommentType( int commentStartLength )
    {
        Throw.CheckOutOfRangeArgument( commentStartLength >= 1 && commentStartLength < 8 );
        return NodeType.TriviaClassBit | (NodeType)commentStartLength;
    }

    public static NodeType GetTriviaBlockCommentType( int commentStartLength, int commentEndLength )
    {
        Throw.CheckOutOfRangeArgument( commentEndLength >= 1 && commentEndLength < 8 );
        return GetTriviaLineCommentType( commentStartLength ) | (NodeType)(commentEndLength << 3);
    }

    internal static int GetTriviaCommentStartLength( this NodeType type ) => IsTrivia( type ) ? ((int)type & 3) : 0;

    internal static int GetTriviaCommentEndLength( this NodeType type ) => IsTrivia( type ) ? (((int)type >> 3) & 3) : 0;
}

