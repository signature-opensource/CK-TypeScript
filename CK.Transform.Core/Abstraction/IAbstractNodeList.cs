using System.Collections.Generic;

namespace CK.Transform.Core;

/// <summary>
/// Defines a read only list of <see cref="AbstractNode"/> that is itself a <see cref="IAbstractNode"/>.
/// <para>
/// Some languages can implement such lists with internal token separators, prefix and/or suffix, etc.
/// </para>
/// </summary>
public interface IAbstractNodeList<out T> : IAbstractNode, IReadOnlyList<T> where T : AbstractNode
{
}
