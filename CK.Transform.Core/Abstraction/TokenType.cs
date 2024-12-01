using CommunityToolkit.HighPerformance.Helpers;
using Microsoft.Extensions.Primitives;
using System;

namespace CK.Transform.Core;

[Flags]
public enum TokenType
{
    /// <summary>
    /// Not a token.
    /// </summary>
    None,

    /// <summary>
    /// There is at most 23 classes of token type.
    /// The last (and smallest one with only 127 possible token types) is the <see cref="TriviaClassNumber"/>.
    /// </summary>
    MaxClassNumber = TriviaClassNumber,

    /// <summary>
    /// Error class number is 0 (this is the signed bit n°31).
    /// This is the very first class.
    /// </summary>
    ErrorClassNumber = 0,

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
    ErrorClassBit = 1 << (31 - ErrorClassNumber),

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
    /// Generic token type are not classified: none of the 24 class bits are set: this lets
    /// 255 possible token types for them.
    /// <para>
    /// Parsers are free to reuse them but often, these generic types won't be used,
    /// they will be redefined in the TokenType Class defined for the language. 
    /// The <c>GenericLowerThan</c> for instance should not be useful to many languages: this
    /// is often an operator with an associated priority and a specific TokenType will be
    /// able to capture the priority.
    /// </para>
    /// <para>
    /// The GenericText can contain any text. This is a kind of fallback token type that can be used
    /// when an island of text doesn't need any special processing or is not handled. An example of
    /// usage is to model the content of an Xml CDATA node.
    /// </para>
    /// </summary>
    GenericText = 1,

    /// <summary>
    /// Can be used by error tolerant parsers to denote an unrecognized token in an error tolerant parser.
    /// <para>
    /// An unrecognized token would be an error in a regular parser.
    /// </para>
    /// </summary>
    GenericUnexpectedToken = 2,

    /// <summary>
    /// Can be used by error tolerant parsers to denote a missing token (for the <see cref="MissingTokenNode"/>).
    /// </summary>
    GenericMissingToken = 3,

    /// <summary>
    /// A comma <c>,</c> is often used with a the only semantics to separate items in list.
    /// Note that when expressions must be analyzed, this may (dependeing on the language and
    /// the way the parser is written) need to be considered as an operator and associated to
    /// a priority.
    /// </summary>
    GenericComma = 4,

    /// <summary>
    /// A semi-colon <c>;</c> is often used as the statement terminator.
    /// </summary>
    GenericSemiColon = 5,

    /// <summary>
    /// A string can have a lot of representations. This can be used by language parsers if
    /// it can avoid creating a specific token type.
    /// </summary>
    GenericString = 6,

    /// <summary>
    /// An identifier can have a lot of representations. This can be used by language parsers if
    /// it can avoid creating a specific token type.
    /// </summary>
    GenericIdentifier = 7,

    /// <summary>
    /// A keyword. This can be used by language parsers if
    /// it can avoid creating a specific token type.
    /// </summary>
    GenericKeyword = 8,

    /// <summary>
    /// The end of input has only the most significant bit set.
    /// This can be understood as the combination of the <see cref="ErrorClassBit"/>
    /// and <see cref="None"/>.
    /// </summary>
    EndOfInput = ErrorClassBit,

    /// <summary>
    /// Generic syntax error.
    /// </summary>
    SyntaxError = ErrorClassBit | 1,

    /// <summary>
    /// Denotes an unhandled token. See <see cref="TokenErrorNode.Unhandled"/>.
    /// </summary>
    ErrorUnhandled = ErrorClassBit | 2,

    /// <summary>
    /// An unterminated string: the end-of-input has been reached before the closing "quote" (whatever it is).
    /// </summary>
    ErrorUnterminatedString = ErrorClassBit | 3,


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


}

