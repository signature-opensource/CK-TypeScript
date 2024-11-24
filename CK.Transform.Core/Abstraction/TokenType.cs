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
    /// The end of input has only the most significant bit set.
    /// </summary>
    EndOfInput = ErrorClassBit,

    /// <summary>
    /// Denotes an unhandled token. See <see cref="TokenErrorNode.Unhandled"/>.
    /// </summary>
    ErrorUnhandled = ErrorClassBit | 3 << 24,

    /// <summary>
    /// An unterminated string: the end-of-input has been reached before the closing quote (whatever it is).
    /// </summary>
    ErrorUnterminatedString = ErrorClassBit | 4 << 24,

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

