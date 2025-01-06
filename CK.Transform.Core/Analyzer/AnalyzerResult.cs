using CK.Core;
using System.Collections.Immutable;

namespace CK.Transform.Core;

/// <summary>
/// Extends <see cref="IAnalyzerResult{T}"/> and provides helpers.
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
