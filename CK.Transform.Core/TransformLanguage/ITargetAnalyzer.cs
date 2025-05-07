using CK.Core;
using System;

namespace CK.Transform.Core;

/// <summary>
/// Target language analyzers must be <see cref="ITokenizerHeadBehavior"/> to
/// handle <see cref="SpanMatcher"/>'s pattern parsing.
/// <para>
/// How match patterns are parsed is under control of <see cref="TransformStatementAnalyzer.ParsePattern"/>.
/// </para>
/// </summary>
public interface ITargetAnalyzer : IAnalyzer, ITokenizerHeadBehavior
{
}
