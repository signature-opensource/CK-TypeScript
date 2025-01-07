using System;

namespace CK.Transform.Core;

/// <summary>
/// Analyzer abstraction.
/// </summary>
public interface IAnalyzer
{
    /// <summary>
    /// Gets the language name handled by this analyzer.
    /// </summary>
    string LanguageName { get; }

    /// <summary>
    /// Parses a text.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>The result.</returns>
    AnalyzerResult Parse( ReadOnlyMemory<char> text );
}
