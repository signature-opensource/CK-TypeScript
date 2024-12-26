using CK.Transform.TransformLanguage;
using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace CK.Transform.Core;

/// <summary>
/// Non generic <see cref="IAnalyzerResult{T}"/>.
/// </summary>
public interface IAnalyzerResult
{
    /// <summary>
    /// Gets whether <see cref="Error"/> is null and no <see cref="TokenError"/> exist in the <see cref="Tokens"/>.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets a potentially complex result if this analyzer is more than a <see cref="Tokenizer"/>.
    /// This is typically the root node of an Abstract Syntax Tree.
    /// </summary>
    object? Result { get; }

    /// <summary>
    /// Gets a "hard failure" error that stopped the analysis.
    /// </summary>
    TokenError? Error { get; }

    /// <summary>
    /// Gets the tokens. Empty if <see cref="Error"/> is not null.
    /// </summary>
    ImmutableArray<Token> Tokens { get; }
}

