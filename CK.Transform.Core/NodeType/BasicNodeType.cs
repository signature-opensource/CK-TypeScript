namespace CK.Transform.Core;

/// <summary>
/// Basic node types are not classified: none of the 24 class bits are set: this lets
/// 255 possible token types for them: this enum is backed by a byte.
/// <para>
/// Parsers are free to reuse them but often, these generic types won't be used,
/// they will be redefined in the TokenType Class defined for the language. 
/// </para>
/// </summary>
public enum BasicNodeType : byte
{
    /// <summary>
    /// Non applicable, undefined or unknown.
    /// </summary>
    None,

    /// <summary>
    /// The GenericNode is the default for <see cref="Core.SyntaxNode"/>.
    /// </summary>
    SyntaxNode,

    /// <summary>
    /// The node type for <see cref="ErrorTolerant.SyntaxErrorNode"/>.
    /// </summary>
    SyntaxErrorNode,

    /// <summary>
    /// The GenericText can contain any text. This is a kind of fallback token type that can be used
    /// when an island of text doesn't need any special processing or is not handled. An example of
    /// usage is to model the content of an Xml CDATA node.
    /// </summary>
    GenericText,

    /// <summary>
    /// Can be used by error tolerant parsers to denote an unrecognized (or skipped) token.
    /// <para>
    /// An unrecognized token would be an error in a regular parser.
    /// </para>
    /// </summary>
    GenericUnexpectedToken,

    /// <summary>
    /// Can be used by error tolerant parsers to denote a missing token (for the <see cref="MissingTokenNode"/>).
    /// </summary>
    GenericMissingToken,

    /// <summary>
    /// A string can have a lot of representations. This can be used by language parsers if
    /// it can avoid creating a specific token type.
    /// </summary>
    GenericString,

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
    /// A comma <c>,</c> is often used with a the only semantics to separate items in list.
    /// Note that when expressions must be analyzed, this may (depending on the language and
    /// the way the parser is written) need to be considered as an operator and associated to
    /// a priority.
    /// </summary>
    Comma,

    /// <summary>
    /// A semi-colon <c>;</c>.
    /// </summary>
    SemiColon,

    /// <summary>
    /// A colon <c>:</c>.
    /// </summary>
    Colon,

    /// <summary>
    /// A double quote <c>"</c>.
    /// </summary>
    DoubleQuote,

    /// <summary>
    /// A single quote <c>'</c>.
    /// </summary>
    SingleQuote,

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
    OpenPar,

    /// <summary>
    /// Closing <c>)</c>.
    /// </summary>
    ClosePar,

    /// <summary>
    /// Opening <c>{</c>.
    /// </summary>
    OpenBrace,

    /// <summary>
    /// Closing <c>}</c>.
    /// </summary>
    CloseBrace,

    /// <summary>
    /// Opening <c>&lt;</c>.
    /// </summary>
    OpenAngleBracket,

    /// <summary>
    /// Closing <c>&lt;</c>.
    /// </summary>
    CloseAngleBracket,
}

