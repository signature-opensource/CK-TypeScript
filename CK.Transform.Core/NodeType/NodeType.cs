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
    /// The last (and smallest one with only 256 possible values) is the <see cref="TriviaClassNumber"/>.
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
    /// This is the token type class n°23 and the last possible class.
    /// See <see cref="Trivia"/>.
    /// </para>
    /// </summary>
    TriviaClassNumber = 23,
    TriviaClassBit = 1 << (31 - TriviaClassNumber),
    TriviaClassMask = -1 << (31 - TriviaClassNumber),

    #region Trivia

    /// <summary>
    /// One or more <see cref="char.IsWhiteSpace(char)"/>.
    /// </summary>
    Whitespace = TriviaClassBit,

    /// <summary>
    /// Currently unused bit. Must be 0.
    /// </summary>
    TriviaUnusedBit = TriviaClassBit | 1 << 6,

    /// <summary>
    /// Mask for the length of the comment start. Use <see cref="NodeTypeExtensions.GetTriviaCommentStartLength(NodeType)"/> 
    /// to retrieve this information.
    /// </summary>
    TriviaCommentStartLengthMask = TriviaClassBit | 3,

    /// <summary>
    /// Mask for the length of the comment start. Use <see cref="NodeTypeExtensions.GetTriviaCommentEndLength(NodeType)"/> 
    /// to retrieve this information.
    /// </summary>
    TriviaCommentEndLengthMask = TriviaClassBit | 7 << 3,

    /// <summary>
    /// Mask for both start and end comment length.
    /// </summary>
    TriviaCommentMask = TriviaCommentStartLengthMask | TriviaCommentEndLengthMask,

    #endregion

    /// <summary>
    /// The end of input has only the most significant bit set.
    /// This can be understood as the combination of the <see cref="ErrorClassBit"/>
    /// and <see cref="None"/>.
    /// Used by <see cref="NodeLocation.EndOfInput"/> and any <see cref="ParserHead.EndOfInput"/> tokens.
    /// </summary>
    EndOfInput = ErrorClassBit,

    /// <summary>
    /// Beginning of the input (the fact that this is clasified as an error is meaningless).
    /// Used by <see cref="NodeLocation.BegOfInput"/>.
    /// </summary>
    BegOfInput = ErrorClassBit | SyntaxErrorNode,

    /// <summary>
    /// An unterminated string: the end-of-input has been reached before the closing "quote" (whatever it is).
    /// </summary>
    ErrorUnterminatedString = ErrorClassBit | GenericString,



    /// <inheritdoc cref="BasicNodeType.SyntaxNode"/>
    SyntaxNode = BasicNodeType.SyntaxNode,
    /// <inheritdoc cref="BasicNodeType.SyntaxErrorNode"/>
    SyntaxErrorNode = BasicNodeType.SyntaxErrorNode,
    /// <inheritdoc cref="BasicNodeType.GenericNode"/>
    GenericNode = BasicNodeType.GenericNode,
    /// <inheritdoc cref="BasicNodeType.GenericUnexpectedToken"/>
    GenericUnexpectedToken = BasicNodeType.GenericUnexpectedToken,
    /// <inheritdoc cref="BasicNodeType.GenericMissingToken"/>
    GenericMissingToken = BasicNodeType.GenericMissingToken,
    /// <inheritdoc cref="BasicNodeType.GenericMarkerToken"/>
    GenericMarkerToken = BasicNodeType.GenericMarkerToken,
    /// <inheritdoc cref="BasicNodeType.GenericString"/>
    GenericString = BasicNodeType.GenericString,
    /// <inheritdoc cref="BasicNodeType.GenericIdentifier"/>
    GenericIdentifier = BasicNodeType.GenericIdentifier,
    /// <inheritdoc cref="BasicNodeType.GenericKeyword"/>
    GenericKeyword = BasicNodeType.GenericKeyword,
    /// <inheritdoc cref="BasicNodeType.GenericInteger"/>
    GenericInteger = BasicNodeType.GenericInteger,
    /// <inheritdoc cref="BasicNodeType.GenericFloat"/>
    GenericFloat = BasicNodeType.GenericFloat,
    /// <inheritdoc cref="BasicNodeType.GenericNumber"/>
    GenericNumber = BasicNodeType.GenericNumber,
    /// <inheritdoc cref="BasicNodeType.GenericRegularExpression"/>
    GenericRegularExpression = BasicNodeType.GenericNumber,
    
    /// <inheritdoc cref="BasicNodeType.OpenBracket"/>
    OpenBracket = BasicNodeType.OpenBracket,
    /// <inheritdoc cref="BasicNodeType.CloseBracket"/>
    CloseBracket = BasicNodeType.CloseBracket,
    /// <inheritdoc cref="BasicNodeType.OpenParen"/>
    OpenParen = BasicNodeType.OpenParen,
    /// <inheritdoc cref="BasicNodeType.CloseParen"/>
    CloseParen = BasicNodeType.CloseParen,
    /// <inheritdoc cref="BasicNodeType.OpenBrace"/>
    OpenBrace = BasicNodeType.OpenBrace,
    /// <inheritdoc cref="BasicNodeType.CloseBrace"/>
    CloseBrace = BasicNodeType.CloseBrace,
    /// <inheritdoc cref="BasicNodeType.LessThan"/>
    LessThan = BasicNodeType.LessThan,
    /// <inheritdoc cref="BasicNodeType.GreaterThan"/>
    GreaterThan = BasicNodeType.GreaterThan,
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
    /// <inheritdoc cref="BasicNodeType.BackTick"/>
    BackTick = BasicNodeType.BackTick,

    Equals = BasicNodeType.Equals,
    PlusEquals = BasicNodeType.PlusEquals,
    MinusEquals = BasicNodeType.MinusEquals,
    AsteriskEquals = BasicNodeType.AsteriskEquals,
    SlashEquals = BasicNodeType.SlashEquals,
    PercentEquals = BasicNodeType.PercentEquals,
    AmpersandEquals = BasicNodeType.AmpersandEquals,
    CaretEquals = BasicNodeType.CaretEquals,
    BarEquals = BasicNodeType.BarEquals,
    LessThanLessThanEquals = BasicNodeType.LessThanLessThanEquals,
    GreaterThanGreaterThanEquals = BasicNodeType.GreaterThanGreaterThanEquals,
    GreaterThanGreaterThanGreaterThanEquals = BasicNodeType.GreaterThanGreaterThanGreaterThanEquals,
    Question = BasicNodeType.Question,
    BarBar = BasicNodeType.BarBar,
    AmpersandAmpersand = BasicNodeType.AmpersandAmpersand,
    Bar = BasicNodeType.Bar,
    Caret = BasicNodeType.Caret,
    EqualsEquals = BasicNodeType.EqualsEquals,
    EqualsEqualsEquals = BasicNodeType.EqualsEqualsEquals,
    LessThanEquals = BasicNodeType.LessThanEquals,
    GreaterThanEquals = BasicNodeType.GreaterThanEquals,
    LessThanLessThan = BasicNodeType.LessThanLessThan,
    LessThanLessThanLessThan = BasicNodeType.LessThanLessThanLessThan,
    GreaterThanGreaterThan = BasicNodeType.GreaterThanGreaterThan,
    GreaterThanGreaterThanGreaterThan = BasicNodeType.GreaterThanGreaterThanGreaterThan,
    Plus = BasicNodeType.Plus,
    Minus = BasicNodeType.Minus,
    Asterisk = BasicNodeType.Asterisk,
    Slash = BasicNodeType.Slash,
    BackSlash = BasicNodeType.BackSlash,
    Percent = BasicNodeType.Percent,
    Dollar = BasicNodeType.Dollar,
    AtSign = BasicNodeType.AtSign,
    Hash = BasicNodeType.Hash,
    Ampersand = BasicNodeType.Ampersand,
    Tilde = BasicNodeType.Tilde,
    Exclamation = BasicNodeType.Exclamation,
    ExclamationEquals = BasicNodeType.ExclamationEquals,
    ExclamationEqualsEquals = BasicNodeType.ExclamationEqualsEquals,
    PlusPlus = BasicNodeType.PlusPlus,
    MinusMinus = BasicNodeType.MinusMinus,
    Dot = BasicNodeType.Dot,
    DotDot = BasicNodeType.DotDot,
    DotDotDot = BasicNodeType.DotDotDot,
    EqualsGreaterThan = BasicNodeType.EqualsGreaterThan,
}

