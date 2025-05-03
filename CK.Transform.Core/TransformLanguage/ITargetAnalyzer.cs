using CK.Core;
using System;

namespace CK.Transform.Core;

/// <summary>
/// Extends <see cref="IAnalyzer"/> to handle <see cref="SpanMatcher"/> parsing.
/// </summary>
public interface ITargetAnalyzer : IAnalyzer
{
    /// <summary>
    /// Creates a <see cref="ITokenFilter"/> from an optional span specication and textual pattern. 
    /// </summary>
    /// <param name="monitor">The monitor to use for errors.</param>
    /// <param name="spanSpec">Span specification. Can be empty, empty enclosed <c>{}</c> or contains any <c>{specification}</c>.</param>
    /// <param name="pattern">The pattern to analyze. Can be empty.</param>
    /// <returns>A token filter on success, null on error.</returns>
    ITokenFilter? CreateSpanMatcher( IActivityMonitor monitor, ReadOnlySpan<char> spanSpec, ReadOnlyMemory<char> pattern );
}
