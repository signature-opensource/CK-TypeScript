using CK.Core;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Transform.Core;

/// <summary>
/// Result of the <see cref="IAnalyzer{T}.Parse(System.ReadOnlyMemory{char})"/>.
/// <para>
/// Parsing errors can be the single <see cref="IAnalyzerResult.Error"/> or multiple errors can appear in the <see cref="IAnalyzerResult.Tokens"/>.
/// </para>
/// </summary>
public interface IAnalyzerResult<out T> : IAnalyzerResult where T : class
{
    /// <inheritdoc cref="IAnalyzerResult.Result" />
    new T? Result { get; }
}
