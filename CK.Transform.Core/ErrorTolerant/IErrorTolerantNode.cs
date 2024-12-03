
namespace CK.Transform.Core;

/// <summary>
/// Marker interface for <see cref="ErrorTolerant.MissingTokenNode"/>, <see cref="ErrorTolerant.UnexpectedTokenNode"/>
/// and <see cref="ErrorTolerant.SyntaxErrorNode"/>.
/// <para>
/// An error tolerant node can only appear in a <see cref="ErrorTolerant.SyntaxErrorNode"/>.
/// </para>
/// </summary>
public interface IErrorTolerantNode : IAbstractNode
{
}
