using System.Linq;

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

    /// <summary>
    /// Gets the first <see cref="TokenError"/> in <see cref="SourceCode.Tokens"/>.
    /// <para>
    /// This is always null if <see cref="Success"/> is true and may still be null if a "hard" <see cref="Error"/>
    /// has been created by the analyzer (and no inline error has been appended).
    /// </para>
    /// </summary>
    public TokenError? FirstInlineError => SourceCode.Tokens.OfType<TokenError>().FirstOrDefault();

}
