using CK.Core;
using System;

namespace CK.Transform.Core;

/// <summary>
/// Target language analyzers must be <see cref="ITokenizerHeadBehavior"/> to
/// handle <see cref="SpanMatcher"/>'s pattern parsing.
/// </summary>
public interface ITargetAnalyzer : IAnalyzer, ITokenizerHeadBehavior
{
}
