using System;

namespace CK.Transform.Core;

/// <summary>
/// Low level token classificator.
/// </summary>
public interface ILowLevelTokenizer
{
    /// <summary>
    /// Gets whether the <see cref="TriviaHead"/> must handle <see cref="TokenType.Whitespace"/>.
    /// As is true for almost all languages except specific languages like Html (where whitespaces
    /// are considered part of textual spans), this is a Default Implementation Methods that returns true.
    /// </summary>
    bool HandleWhiteSpaceTrivias => true;

    /// <summary>
    /// The <see cref="LowLevelToken"/> builder.
    /// </summary>
    /// <param name="head">The start of the text to categorize. Leading trivias have already been handled.</param>
    /// <returns>A candidate token.</returns>
    LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head );
}

