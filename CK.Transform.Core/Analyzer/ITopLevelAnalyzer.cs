using System;

namespace CK.Transform.Core;

/// <summary>
/// A top-level analyzer must be able to recognize top-level : a call
/// to <see cref="IAnalyzer.Parse(ReadOnlyMemory{char})"/> can return
/// a result with at most one span that is a <see cref="TopLevelSourceSpan"/>
/// and stops the parsing after it: <see cref="AnalyzerResult.RemainingText"/>
/// is available for subsequent analysis.
/// <para>
/// The <see cref="AnalyzerExtensions.TryParseMultiple{T}(ITopLevelAnalyzer{T}, CK.Core.IActivityMonitor, ReadOnlyMemory{char})"/>
/// does the job once for all to parse multiple top-level elements.
/// </para>
/// <para>
/// Note that a top-level analyzer is not restricted to parse such top-level constructs.
/// The <c>TryParseTopLevel</c> is a helper that can be used only when the text is known to (or must)
/// contain top-level elements.
/// </para>
/// </summary>
/// <typeparam name="T">The type (or base type) of the top-level source span.</typeparam>
public interface ITopLevelAnalyzer<out T> : IAnalyzer where T: TopLevelSourceSpan
{
}

