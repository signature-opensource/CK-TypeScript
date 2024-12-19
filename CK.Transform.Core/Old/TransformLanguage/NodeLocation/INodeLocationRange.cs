using System.Collections.Generic;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Generalization of a range: it is always an enumerable of <see cref="NodeLocationRange"/>
/// or an actual NodeLocationRange.
/// This list does not contain null ranges and ranges are necessarily non empty and follow an ascending order.
/// </summary>
public interface INodeLocationRange : IEnumerable<NodeLocationRange>
{
    /// <summary>
    /// Gets the number of <see cref="NodeLocationRange"/> that this range contains.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the first <see cref="NodeLocationRange"/>.
    /// </summary>
    NodeLocationRange First { get; }

    /// <summary>
    /// Gets the last <see cref="NodeLocationRange"/>.
    /// </summary>
    NodeLocationRange Last { get; }
}

