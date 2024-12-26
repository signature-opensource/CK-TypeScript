using System;

namespace CK.Transform.Core;

/// <summary>
/// Low level token classificator.
/// </summary>
public interface ILowLevelTokenizer
{
    /// <summary>
    /// The <see cref="LowLevelToken"/> builder.
    /// </summary>
    /// <param name="head">The start of the text to categorize. Leading trivias have already been handled.</param>
    /// <returns>A candidate token.</returns>
    LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head );
}

