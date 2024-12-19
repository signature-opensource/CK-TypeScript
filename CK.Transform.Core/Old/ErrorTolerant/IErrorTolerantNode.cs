
using CK.Transform.Core;

namespace CK.Transform.ErrorTolerant;

/// <summary>
/// Marker interface for <see cref="MissingTokenNode"/>, <see cref="UnexpectedTokenNode"/>
/// and <see cref="SyntaxErrorNode"/>.
/// <para>
/// An error tolerant node can only appear in a <see cref="SyntaxErrorNode"/>.
/// </para>
/// </summary>
public interface IErrorTolerantNode : IAbstractNode
{
}
