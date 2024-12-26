using System;

namespace CK.Transform.Core;

/// <summary>
/// An Analyzer produces a <see cref="IAnalyzerResult{T}"/> from a text.
/// <para>
/// This result may be a single <see cref="IAnalyzerResult.Error"/>, the <see cref="IAnalyzerResult.Tokens"/>
/// (that can contain inlined errors) and may have <see cref="IAnalyzerResult{T}.Result"/>.
/// </para>
/// </summary>
/// <typeparam name="T">The result type.</typeparam>
public interface IAnalyzer<out T> : IAnalyzer where T : class
{
    /// <inheritdoc cref="IAnalyzer.Parse(ReadOnlyMemory{char})"/>
    new IAnalyzerResult<T> Parse( ReadOnlyMemory<char> text );
}
