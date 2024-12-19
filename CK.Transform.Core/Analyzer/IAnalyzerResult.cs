using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Transform.Core;


/// <summary>
/// An Analyzer can produce a <see cref="IAnalyzed"/> from a text.
/// </summary>
/// <typeparam name="T">The result type.</typeparam>
public interface IAnalyzer<out T> where T : class, IAnalyzed
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    IAnalyzerOutcome<T> Parse( ReadOnlyMemory<char> text );
}

/// <summary>
/// Non generic root for <see cref="IAnalyzer{T}"/>.
/// </summary>
public interface IAnalyzer : IAnalyzer<IAnalyzed>
{
}

/// <summary>
/// The result of an analyzer can be anything that exposes its tokens.
/// </summary>
public interface IAnalyzed
{
    /// <summary>
    /// Gets the tokens.
    /// </summary>
    IReadOnlyList<Token> AllTokens { get; }
}

/// <summary>
/// The result of an analyzer can be anything that exposes its tokens.
/// </summary>
public interface IAnalyzerOutcome<out T> where T : class, IAnalyzed
{
    T? Result { get; }
    TokenErrorNode? Error { get; }
    ImmutableArray<Token> Tokens { get; }
}
