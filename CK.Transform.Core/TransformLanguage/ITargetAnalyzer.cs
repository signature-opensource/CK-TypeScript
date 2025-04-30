using CK.Core;
using System;

namespace CK.Transform.Core;

/// <summary>
/// Extends <see cref="IAnalyzer"/> to handle <see cref="SpanMatcher"/> parsing.
/// </summary>
public interface ITargetAnalyzer : IAnalyzer
{
    /// <summary>
    /// Creates a <see cref="ITokenFilter"/> from an optional span type and a textual pattern. 
    /// </summary>
    /// <param name="monitor">The monitor to use for errors.</param>
    /// <param name="spanType">Span type (can be empty).</param>
    /// <param name="pattern">The pattern to analyze. Never empty.</param>
    /// <returns>A token filter on success, null on error.</returns>
    ITokenFilter? CreateSpanMatcher( IActivityMonitor monitor, ReadOnlySpan<char> spanType, ReadOnlyMemory<char> pattern );
}
