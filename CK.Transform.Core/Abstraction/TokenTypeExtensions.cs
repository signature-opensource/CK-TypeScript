using CK.Core;

namespace CK.Transform.Core;

public static class TokenTypeExtensions
{
    static string?[] _classNames;

    static TokenTypeExtensions()
    {
        _classNames = new string?[(int)TokenType.MaxClassNumber + 1];
        _classNames[0] = "Error";
        _classNames[(int)TokenType.TriviaClassNumber] = "Trivia";
    }

    /// <summary>
    /// Reserves a token class.
    /// </summary>
    /// <param name="classNumber">The number to reserve.</param>
    /// <param name="tokenClassName">The token class name.</param>
    public static void ReserveTokenClass( int classNumber, string tokenClassName )
    {
        Throw.CheckOutOfRangeArgument( classNumber >= 0 && classNumber <= (int)TokenType.MaxClassNumber );
        Throw.CheckNotNullOrWhiteSpaceArgument( tokenClassName );
        var already = _classNames[classNumber];
        if( already != null )
        {
            Throw.InvalidOperationException( $"The class '{tokenClassName}' cannot use n°{classNumber}, this number is already reserved by '{already}'." );
        }
        _classNames[classNumber] = tokenClassName;
    }


    /// <summary>
    /// Gets whether this is <see cref="TokenType.None"/> or <see cref="TokenType.ErrorClassBit"/> is set.
    /// <para>
    /// Only <see cref="TokenErrorNode"/> can carry an error token type.
    /// </para>
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is an error.</returns>
    public static bool IsError( this TokenType type ) => type <= 0;

    /// <summary>
    /// Gets whether this token type is actually a successful kind of <see cref="Trivia"/>, including
    /// <see cref="TokenType.Whitespace"/>.
    /// <para>
    /// Only Trivia (not <see cref="TokenNode"/>) can carry a trivia token type.
    /// </para>
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a trivia.</returns>
    public static bool IsTrivia( this TokenType type ) => (type & TokenType.TriviaClassMask) == TokenType.TriviaClassMask;

}

