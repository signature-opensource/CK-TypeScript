namespace CK.Transform.Core;

/// <summary>
/// Marks an error node. This applies to the "real" error <see cref="TokenErrorNode"/> but
/// also to the <see cref="ErrorTolerant.IErrorTolerantNode"/> nodes.
/// <para>
/// For a non error tolerant analyzer, any IErrorNode is an error that is lifted to
/// the caller.
/// </para>
/// </summary>
public interface IErrorNode
{
}
