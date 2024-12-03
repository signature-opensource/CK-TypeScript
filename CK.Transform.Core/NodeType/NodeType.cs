using CommunityToolkit.HighPerformance.Helpers;
using Microsoft.Extensions.Primitives;
using System;

namespace CK.Transform.Core;


[Flags]
public enum NodeType
{
    /// <summary>
    /// Not a token.
    /// </summary>
    None = 0,

    /// <summary>
    /// There is at most 23 classes of token type.
    /// The last (and smallest one with only 127 possible token types) is the <see cref="TriviaClassNumber"/>.
    /// <para>
    /// Error class number is 0 (this is the signed bit n°31).
    /// </para>
    /// </summary>
    MaxClassNumber = TriviaClassNumber,

    /// <summary>
    /// The mask to consider when challenging classes). 
    /// </summary>
    ClassMask = -1 << (31 - MaxClassNumber),

    /// <summary>
    /// Sign bit (bit n°31) is 1 to indicate an error or the end of the input.
    /// <para>
    /// This is the token type class n°0 and the only class that can be combined with
    /// another class bit (with any other token type). Thanks to this, any token type
    /// can be "on error". Trivia uses this: the error for a type of comment is the
    /// "unterminated comment" error.
    /// </para>
    /// </summary>
    ErrorClassBit = 1 << 31,

    /// <summary>
    /// Class for trivias (whitespace and comments).
    /// <para>
    /// This is the token type class n°23 and the last possible class. Even if we currently
    /// have only 5 types of trivias, there is a provision for 127 total types of trivias.
    /// May be useful if in the future we add new types of trivias (or offer an extension points for trivias). 
    /// </para>
    /// </summary>
    TriviaClassNumber = 23,
    TriviaClassBit = 1 << TriviaClassNumber,
    TriviaClassMask = -1 << (31 - TriviaClassNumber),

    /// <summary>
    /// The end of input has only the most significant bit set.
    /// This can be understood as the combination of the <see cref="ErrorClassBit"/>
    /// and <see cref="None"/>.
    /// </summary>
    EndOfInput = ErrorClassBit,

    /// <summary>
    /// Denotes an unhandled token. See <see cref="TokenErrorNode.Unhandled"/>.
    /// </summary>
    ErrorUnhandled = ErrorClassBit | GenericText,

    /// <summary>
    /// An unterminated string: the end-of-input has been reached before the closing "quote" (whatever it is).
    /// </summary>
    ErrorUnterminatedString = ErrorClassBit | GenericString,


    #region Trivia

    /// <summary>
    /// One or more <see cref="char.IsWhiteSpace(char)"/>.
    /// </summary>
    Whitespace = TriviaClassBit,

    /// <summary>
    /// The "//".
    /// Ends with a new line.
    /// </summary>
    LineComment = TriviaClassBit | 1,

    /// <summary>
    /// The "/*".
    /// Ends with a "*/".
    /// </summary>
    StarComment = TriviaClassBit | 2,

    /// <summary>
    /// The "&lt;!--".
    /// Ends with "--&gt;".
    /// </summary>
    XmlComment = TriviaClassBit | 3,

    /// <summary>
    /// The "--".
    /// Ends with a new line.
    /// </summary>
    SqlComment = TriviaClassBit | 4,

    #endregion


    SyntaxNode = BasicNodeType.SyntaxNode,
    SyntaxErrorNode = BasicNodeType.SyntaxErrorNode,
    GenericText = BasicNodeType.GenericText,
    GenericUnexpectedToken = BasicNodeType.GenericUnexpectedToken,
    GenericMissingToken = BasicNodeType.GenericMissingToken,
    GenericString = BasicNodeType.GenericString,
    GenericIdentifier = BasicNodeType.GenericIdentifier,
    GenericKeyword = BasicNodeType.GenericKeyword,
    GenericInteger = BasicNodeType.GenericInteger,
    GenericFloat = BasicNodeType.GenericFloat,
    GenericNumber = BasicNodeType.GenericNumber,
    /// <inheritdoc cref="BasicNodeType.Comma"/>
    Comma = BasicNodeType.Comma,
    /// <inheritdoc cref="BasicNodeType.SemiColon"/>
    SemiColon = BasicNodeType.SemiColon,
    /// <inheritdoc cref="BasicNodeType.Colon"/>
    Colon = BasicNodeType.Colon,
    /// <inheritdoc cref="BasicNodeType.DoubleQuote"/>
    DoubleQuote = BasicNodeType.DoubleQuote,
    /// <inheritdoc cref="BasicNodeType.SingleQuote"/>
    SingleQuote = BasicNodeType.SingleQuote,
    /// <inheritdoc cref="BasicNodeType.OpenBracket"/>
    OpenBracket = BasicNodeType.OpenBracket,
    /// <inheritdoc cref="BasicNodeType.CloseBracket"/>
    CloseBracket = BasicNodeType.CloseBracket,
    /// <inheritdoc cref="BasicNodeType.OpenPar"/>
    OpenPar = BasicNodeType.OpenPar,
    /// <inheritdoc cref="BasicNodeType.ClosePar"/>
    ClosePar = BasicNodeType.ClosePar,
    /// <inheritdoc cref="BasicNodeType.OpenBrace"/>
    OpenBrace = BasicNodeType.OpenBrace,
    /// <inheritdoc cref="BasicNodeType.CloseBrace"/>
    CloseBrace = BasicNodeType.CloseBrace,
    /// <inheritdoc cref="BasicNodeType.OpenAngleBracket"/>
    OpenAngleBracket = BasicNodeType.OpenAngleBracket,
    /// <inheritdoc cref="BasicNodeType.CloseAngleBracket"/>
    CloseAngleBracket = BasicNodeType.CloseAngleBracket,
}

