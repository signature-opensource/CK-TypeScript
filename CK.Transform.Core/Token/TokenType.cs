using System;

namespace CK.Transform.Core;

[Flags]
public enum TokenType
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
    /// A generic error type.
    /// </summary>
    GenericError = BasicTokenType.GenericAny | ErrorClassBit,

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
    /// Mask for the length of the comment start. Use <see cref="TokenTypeExtensions.GetTriviaCommentStartLength(TokenType)"/> 
    /// to retrieve this information.
    /// </summary>
    TriviaCommentStartLengthMask = TriviaClassBit | 3,

    /// <summary>
    /// Mask for the length of the comment start. Use <see cref="TokenTypeExtensions.GetTriviaCommentEndLength(TokenType)"/> 
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

    /// <inheritdoc cref="BasicTokenType.SyntaxNode"/>
    SyntaxNode = BasicTokenType.SyntaxNode,
    /// <inheritdoc cref="BasicTokenType.SyntaxErrorNode"/>
    SyntaxErrorNode = BasicTokenType.SyntaxErrorNode,
    /// <inheritdoc cref="BasicTokenType.GenericAny"/>
    GenericNode = BasicTokenType.GenericAny,
    /// <inheritdoc cref="BasicTokenType.GenericUnexpectedToken"/>
    GenericUnexpectedToken = BasicTokenType.GenericUnexpectedToken,

    /// <inheritdoc cref="BasicTokenType.GenericMissingToken"/>
    GenericMissingToken = BasicTokenType.GenericMissingToken,
    /// <inheritdoc cref="BasicTokenType.GenericMarkerToken"/>
    GenericMarkerToken = BasicTokenType.GenericMarkerToken,

    /// <inheritdoc cref="BasicTokenType.GenericString"/>
    GenericString = BasicTokenType.GenericString,
    /// <inheritdoc cref="BasicTokenType.GenericInterpolatedStringStart"/>
    GenericInterpolatedStringStart,
    /// <inheritdoc cref="BasicTokenType.GenericInterpolatedStringSegment"/>
    GenericInterpolatedStringSegment,
    /// <inheritdoc cref="BasicTokenType.GenericInterpolatedStringEnd"/>
    GenericInterpolatedStringEnd,

    /// <inheritdoc cref="BasicTokenType.GenericIdentifier"/>
    GenericIdentifier = BasicTokenType.GenericIdentifier,
    /// <inheritdoc cref="BasicTokenType.GenericKeyword"/>
    GenericKeyword = BasicTokenType.GenericKeyword,
    /// <inheritdoc cref="BasicTokenType.GenericInteger"/>
    GenericInteger = BasicTokenType.GenericInteger,
    /// <inheritdoc cref="BasicTokenType.GenericFloat"/>
    GenericFloat = BasicTokenType.GenericFloat,
    /// <inheritdoc cref="BasicTokenType.GenericNumber"/>
    GenericNumber = BasicTokenType.GenericNumber,
    /// <inheritdoc cref="BasicTokenType.GenericRegularExpression"/>
    GenericRegularExpression = BasicTokenType.GenericNumber,
    
    /// <inheritdoc cref="BasicTokenType.OpenBracket"/>
    OpenBracket = BasicTokenType.OpenBracket,
    /// <inheritdoc cref="BasicTokenType.CloseBracket"/>
    CloseBracket = BasicTokenType.CloseBracket,
    /// <inheritdoc cref="BasicTokenType.OpenParen"/>
    OpenParen = BasicTokenType.OpenParen,
    /// <inheritdoc cref="BasicTokenType.CloseParen"/>
    CloseParen = BasicTokenType.CloseParen,
    /// <inheritdoc cref="BasicTokenType.OpenBrace"/>
    OpenBrace = BasicTokenType.OpenBrace,
    /// <inheritdoc cref="BasicTokenType.CloseBrace"/>
    CloseBrace = BasicTokenType.CloseBrace,

    Ampersand = BasicTokenType.Ampersand,
    AmpersandEquals = BasicTokenType.AmpersandEquals,
    AmpersandAmpersand = BasicTokenType.AmpersandAmpersand,
    AmpersandAmpersandEquals = BasicTokenType.AmpersandAmpersandEquals,

    Asterisk = BasicTokenType.Asterisk,
    AsteriskEquals = BasicTokenType.AsteriskEquals,

    AtSign = BasicTokenType.AtSign,

    BackSlash = BasicTokenType.BackSlash,
    /// <inheritdoc cref="BasicTokenType.BackTick"/>
    BackTick = BasicTokenType.BackTick,

    Bar = BasicTokenType.Bar,
    BarBar = BasicTokenType.BarBar,
    BarEquals = BasicTokenType.BarEquals,
    BarBarEquals = BasicTokenType.BarBarEquals,

    Caret = BasicTokenType.Caret,
    CaretEquals = BasicTokenType.CaretEquals,

    /// <inheritdoc cref="BasicTokenType.Colon"/>
    Colon = BasicTokenType.Colon,
    /// <inheritdoc cref="BasicTokenType.Comma"/>
    Comma = BasicTokenType.Comma,

    Dollar = BasicTokenType.Dollar,
    /// <inheritdoc cref="BasicTokenType.DoubleQuote"/>
    DoubleQuote = BasicTokenType.DoubleQuote,

    Dot = BasicTokenType.Dot,
    DotDot = BasicTokenType.DotDot,
    DotDotDot = BasicTokenType.DotDotDot,

    Equals = BasicTokenType.Equals,
    EqualsEquals = BasicTokenType.EqualsEquals,
    EqualsEqualsEquals = BasicTokenType.EqualsEqualsEquals,
    EqualsGreaterThan = BasicTokenType.EqualsGreaterThan,

    Exclamation = BasicTokenType.Exclamation,
    ExclamationEquals = BasicTokenType.ExclamationEquals,
    ExclamationEqualsEquals = BasicTokenType.ExclamationEqualsEquals,

    /// <inheritdoc cref="BasicTokenType.GreaterThan"/>
    GreaterThan = BasicTokenType.GreaterThan,
    GreaterThanEquals = BasicTokenType.GreaterThanEquals,
    GreaterThanGreaterThan = BasicTokenType.GreaterThanGreaterThan,
    GreaterThanGreaterThanEquals = BasicTokenType.GreaterThanGreaterThanEquals,
    GreaterThanGreaterThanGreaterThan = BasicTokenType.GreaterThanGreaterThanGreaterThan,
    GreaterThanGreaterThanGreaterThanEquals = BasicTokenType.GreaterThanGreaterThanGreaterThanEquals,

    Hash = BasicTokenType.Hash,

    /// <inheritdoc cref="BasicTokenType.LessThan"/>
    LessThan = BasicTokenType.LessThan,
    LessThanEquals = BasicTokenType.LessThanEquals,
    LessThanLessThan = BasicTokenType.LessThanLessThan,
    LessThanLessThanEquals = BasicTokenType.LessThanLessThanEquals,
    LessThanLessThanLessThan = BasicTokenType.LessThanLessThanLessThan,

    Minus = BasicTokenType.Minus,
    MinusMinus = BasicTokenType.MinusMinus,
    MinusEquals = BasicTokenType.MinusEquals,

    Percent = BasicTokenType.Percent,
    PercentEquals = BasicTokenType.PercentEquals,

    Plus = BasicTokenType.Plus,
    PlusPlus = BasicTokenType.PlusPlus,
    PlusEquals = BasicTokenType.PlusEquals,

    Question = BasicTokenType.Question,
    QuestionQuestionEquals = BasicTokenType.QuestionQuestionEquals,

    /// <inheritdoc cref="BasicTokenType.SemiColon"/>
    SemiColon = BasicTokenType.SemiColon,

    /// <inheritdoc cref="BasicTokenType.SingleQuote"/>
    SingleQuote = BasicTokenType.SingleQuote,

    Slash = BasicTokenType.Slash,
    SlashEquals = BasicTokenType.SlashEquals,

    Tilde = BasicTokenType.Tilde,
}

