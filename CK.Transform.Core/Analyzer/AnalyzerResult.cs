namespace CK.Transform.Core;

/// <summary>
/// Captures the result of a <see cref="IAnalyzer.Parse(System.ReadOnlyMemory{char})"/> call.
/// </summary>
public sealed class AnalyzerResult
{
    internal AnalyzerResult( bool success, SourceCode result, TokenError? error )
    {
        Success = success;
        SourceCode = result;
        Error = error;
    }

    /// <summary>
    /// Gets whether <see cref="Error"/> is null and no <see cref="TokenError"/> exist in the <see cref="SourceCode.Tokens"/>.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the result with the tokens and source spans.
    /// </summary>
    public SourceCode SourceCode { get; }

    /// <summary>
    /// Gets a "hard failure" error that stopped the analysis.
    /// </summary>
    public TokenError? Error { get; }

}
