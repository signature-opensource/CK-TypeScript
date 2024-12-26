using System;

namespace CK.Transform.Core;

/// <summary>
/// Non generic of for <see cref="IAnalyzer{T}"/>.
/// </summary>
public interface IAnalyzer
{
    /// <summary>
    /// Parses a text.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>The result.</returns>
    IAnalyzerResult Parse( ReadOnlyMemory<char> text );
}
