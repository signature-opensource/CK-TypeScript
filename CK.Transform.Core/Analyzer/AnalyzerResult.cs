using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Captures the result of a <see cref="IAnalyzer.Parse(System.ReadOnlyMemory{char})"/> call.
/// </summary>
public sealed class AnalyzerResult
{
    internal AnalyzerResult( SourceCode result,
                             TokenError? hardError,
                             TokenError? firstParseError,
                             TokenError? firstAnyError,
                             int errorCount,
                             IReadOnlyList<BindingError>? bindingErrors,
                             ReadOnlyMemory<char> remainingText,
                             bool endOfInput )
    {
        SourceCode = result;
        HardError = hardError;
        FirstParseError = firstParseError;
        FirstAnyError = hardError ?? firstAnyError;
        BindingErrors = bindingErrors ?? [];
        RemainingText = remainingText;
        EndOfInput = endOfInput;
        TotalErrorCount = BindingErrors.Count
                          + (hardError == null
                              ? errorCount
                              : errorCount + 1);
        Throw.DebugAssert( (TotalErrorCount == 0) == (FirstAnyError == null) );
    }

    /// <summary>
    /// Gets whether <see cref="HardError"/> is null, no <see cref="TokenError"/> exist in the <see cref="SourceCode.Tokens"/>
    /// and <see cref="BindingErrors"/> is empty.
    /// </summary>
    [MemberNotNullWhen( false, nameof( FirstAnyError ) )]
    public bool Success => TotalErrorCount == 0;

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
    /// Gets a "hard failure" error that stopped the analysis.
    /// </summary>
    public TokenError? HardError { get; }

    /// <summary>
    /// Gets the first <see cref="TokenError"/> in <see cref="SourceCode.Tokens"/>.
    /// <para>
    /// This is always null if <see cref="Success"/> is true and may still be null if <see cref="HardError"/>
    /// has been returned by the tokenizer (and no error has been appended) or if there are <see cref="BindingErrors"/>.
    /// </para>
    /// </summary>
    public TokenError? FirstParseError { get; }

    /// <summary>
    /// Gets the first error: the <see cref="HardError"/>, the <see cref="FirstParseError"/> or the first <see cref="BindingErrors"/>.
    /// </summary>
    public TokenError? FirstAnyError { get; }

    /// <summary>
    /// Gets the binding errors if any.
    /// </summary>
    public IReadOnlyList<BindingError> BindingErrors { get; }

    /// <summary>
    /// Gets the number of <see cref="TokenError"/> in <see cref="SourceCode.Tokens"/>, plus the <see cref="BindingErrors"/> count
    /// plus one if <see cref="HardError"/> is not null.
    /// </summary>
    public int TotalErrorCount { get; }
}
