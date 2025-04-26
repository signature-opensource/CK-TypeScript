namespace CK.Transform.Core;

/// <summary>
/// Basic token types are not classified: none of the 24 class bits are set: this lets
/// 255 possible token types for them: this enum is backed by the byte <see cref="BasicTokenType"/>.
/// <para>
/// Parsers are free to reuse them even if these generic types can be redefined in the
/// TokenType Class defined for the language. 
/// </para>
/// </summary>
public enum BasicTokenType : byte
{
    /// <summary>
    /// Non applicable, undefined or unknown.
    /// </summary>
    None,

    /// <summary>
    /// Can denote any kind of token.
    /// </summary>
    GenericAny,

    /// <summary>
    /// Can be used by error tolerant parsers to denote an unrecognized (or skipped) token.
    /// </summary>
    GenericUnexpectedToken,

    /// <summary>
    /// Can be used by error tolerant parsers to denote a missing token.
    /// </summary>
    GenericMissingToken,

    /// <summary>
    /// Default type for a marker (can be anything).
    /// </summary>
    GenericMarkerToken,

    /// <summary>
    /// A string can have a lot of representations. This can be used by language parsers if
    /// it can avoid creating a specific token type.
    /// </summary>
    GenericString,

    /// <summary>
    /// Generic interpolated string start: the first part from the delimiter to the first code part.
    /// </summary>
    GenericInterpolatedStringStart,

    /// <summary>
    /// Generic interpolated string segment: between two code parts.
    /// </summary>
    GenericInterpolatedStringSegment,

    /// <summary>
    /// Generic interpolated string segment: from the last code part to the ending delimiter.
    /// </summary>
    GenericInterpolatedStringEnd,

    /// <summary>
    /// An identifier can have a lot of representations. This can be used by language parsers if
    /// it can avoid creating a specific token type.
    /// </summary>
    GenericIdentifier,

    /// <summary>
    /// A keyword. This can be used by language parsers if
    /// it can avoid creating a specific token type.
    /// </summary>
    GenericKeyword,

    /// <summary>
    /// An integer. This can be used by language parsers if
    /// it can avoid creating a specific token type.
    /// </summary>
    GenericInteger,

    /// <summary>
    /// A numerical floating value. This can be used by language parsers if
    /// it can avoid creating a specific token type.
    /// </summary>
    GenericFloat,

    /// <summary>
    /// A numerical value. This can be used by language parsers if
    /// it can avoid creating a specific token type.
    /// </summary>
    GenericNumber,

    /// <summary>
    /// A regular expression. This can be used by language parsers if
    /// it can avoid creating a specific token type.
    /// </summary>
    GenericRegularExpression,

    /// <summary>
    /// Opening <c>[</c>.
    /// </summary>
    OpenBracket,

    /// <summary>
    /// Closing <c>]</c>.
    /// </summary>
    CloseBracket,

    /// <summary>
    /// Opening <c>(</c>.
    /// </summary>
    OpenParen,

    /// <summary>
    /// Closing <c>)</c>.
    /// </summary>
    CloseParen,

    /// <summary>
    /// Opening <c>{</c>.
    /// </summary>
    OpenBrace,

    /// <summary>
    /// Closing <c>}</c>.
    /// </summary>
    CloseBrace,

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    Ampersand,
    AmpersandEquals,
    AmpersandAmpersand,
    /// <summary>
    /// <c>&amp;&amp;=</c> has been introduces in ECMAScript 2020.
    /// </summary>
    AmpersandAmpersandEquals,

    AtSign,

    Asterisk,
    AsteriskEquals,

    Bar,
    BarEquals,
    BarBar,
    /// <summary>
    /// <c>||=</c> has been introduced in ECMAScript 2020.
    /// </summary>
    BarBarEquals, 

    BackSlash,
    /// <summary>
    /// The back tick <c>`</c>.
    /// </summary>

    BackTick,

    Caret,
    CaretEquals,

    /// <summary>
    /// A colon <c>:</c>.
    /// </summary>
    Colon,

    /// <summary>
    /// A double colon <c>::</c> (C++ namespace separator).
    /// </summary>
    ColonColon,

    /// <summary>
    /// A comma <c>,</c> is often used with a the only semantics to separate items in list.
    /// Note that when expressions must be analyzed, this may (depending on the language and
    /// the way the parser is written) need to be considered as an operator (with an
    /// associated priority).
    /// </summary>
    Comma,

    Dollar,

    Dot,
    DotDot,
    DotDotDot,

    /// <summary>
    /// A double quote <c>"</c>.
    /// </summary>
    DoubleQuote,

    Equals,
    EqualsEquals,
    EqualsEqualsEquals,
    EqualsGreaterThan,

    Exclamation,
    ExclamationEquals,
    ExclamationEqualsEquals,

    /// <summary>
    /// GreaterThan <c>&gt;</c> (could have been CloseAngleBracket).
    /// </summary>
    GreaterThan,
    GreaterThanGreaterThanEquals,
    GreaterThanGreaterThanGreaterThanEquals,
    GreaterThanEquals,
    GreaterThanGreaterThan,
    GreaterThanGreaterThanGreaterThan,

    Hash,

    /// <summary>
    /// LessThan <c>&lt;</c> (could have been OpenAngleBracket).
    /// </summary>
    LessThan,
    LessThanEquals,
    LessThanLessThan,
    LessThanLessThanEquals,
    LessThanLessThanLessThan,

    Minus,
    MinusEquals,
    MinusMinus,

    Percent,
    PercentEquals,

    Plus,
    PlusEquals,
    PlusPlus,

    Question,
    QuestionQuestionEquals,

    /// <summary>
    /// A semi-colon <c>;</c>.
    /// </summary>
    SemiColon,

    /// <summary>
    /// A single quote <c>'</c>.
    /// </summary>
    SingleQuote,

    Slash,
    SlashEquals,

    Tilde,
}

