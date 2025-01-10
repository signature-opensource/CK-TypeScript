using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Captures the result of a <see cref="IAnalyzer.Parse(System.ReadOnlyMemory{char})"/> call.
/// </summary>
public sealed class AnalyzerResult
{
    internal AnalyzerResult( SourceCode result, TokenError? hardError, TokenError? firstError, int errorCount )
    {
        SourceCode = result;
        HardError = hardError;
        FirstError = firstError;
        TotalErrorCount = hardError == null ? errorCount : errorCount + 1;
    }

    /// <summary>
    /// Gets whether <see cref="HardError"/> is null and no <see cref="TokenError"/> exist in the <see cref="SourceCode.Tokens"/>.
    /// </summary>
    public bool Success => TotalErrorCount == 0;

    /// <summary>
    /// Gets the result with the tokens and source spans.
    /// </summary>
    public SourceCode SourceCode { get; }

    /// <summary>
    /// Gets a "hard failure" error that stopped the analysis.
    /// </summary>
    public TokenError? HardError { get; }

    /// <summary>
    /// Gets the first <see cref="TokenError"/> in <see cref="SourceCode.Tokens"/>.
    /// <para>
    /// This is always null if <see cref="Success"/> is true and may still be null if <see cref="HardError"/>
    /// has been returned by the tokenizer (and no error has been appended).
    /// </para>
    /// </summary>
    public TokenError? FirstError { get; }

    /// <summary>
    /// Gets the number of <see cref="TokenError"/> in <see cref="SourceCode.Tokens"/> plus one if <see cref="HardError"/> is not null.
    /// </summary>
    public int TotalErrorCount { get; }
}
