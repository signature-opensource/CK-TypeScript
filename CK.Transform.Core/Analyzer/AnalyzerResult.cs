using CK.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

/// <summary>
/// Captures the result of a <see cref="IAnalyzer.Parse(System.ReadOnlyMemory{char})"/> call.
/// </summary>
public sealed class AnalyzerResult
{
    internal AnalyzerResult( SourceCode result,
                             TokenError? firstParseError,
                             int errorCount,
                             ReadOnlyMemory<char> remainingText,
                             bool endOfInput )
    {
        SourceCode = result;
        FirstError = firstParseError;
        RemainingText = remainingText;
        EndOfInput = endOfInput;
        ErrorCount = errorCount;
        Throw.DebugAssert( (ErrorCount == 0) == (FirstError == null) );
    }

    /// <summary>
    /// Gets whether no error occurred.
    /// </summary>
    [MemberNotNullWhen( false, nameof( FirstError ) )]
    public bool Success => ErrorCount == 0;

    /// <summary>
    /// Gets the result with the tokens and source spans.
    /// </summary>
    public SourceCode SourceCode { get; }

    /// <summary>
    /// Gets the remaining text that has not been parsed.
    /// </summary>
    public ReadOnlyMemory<char> RemainingText { get; }

    /// <summary>
    /// Gets whether the end of the input has been reached.
    /// This is true even if <see cref="RemainingText"/> is not empty when only whitespaces and comments
    /// exist.
    /// </summary>
    public bool EndOfInput { get; }

    /// <summary>
    /// Gets the first <see cref="TokenError"/> in <see cref="SourceCode.Tokens"/>.
    /// </summary>
    public TokenError? FirstError { get; }

    /// <summary>
    /// Gets the number of <see cref="TokenError"/> in <see cref="SourceCode.Tokens"/>.
    /// </summary>
    public int ErrorCount { get; }
}
