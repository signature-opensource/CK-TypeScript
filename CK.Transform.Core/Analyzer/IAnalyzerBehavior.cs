using System;

namespace CK.Transform.Core;

/// <summary>
/// Low level <see cref="AnalyzerHead"/> behavior handles the trivias and
/// estimates the <see cref="AnalyzerHead.LowLevelToken"/>.
/// </summary>
public interface IAnalyzerBehavior
{
    /// <summary>
    /// The default <see cref="TriviaParser"/> to apply.
    /// </summary>
    /// <param name="c">The trivia collector.</param>
    void ParseTrivia( ref TriviaHead c );

    /// <summary>
    /// The <see cref="AnalyzerHead.LowLevelToken"/> builder.
    /// </summary>
    /// <param name="head">The start of the text to categorize. Leading trivias have already been handled.</param>
    /// <returns>A candidate token.</returns>
    LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head );
}

