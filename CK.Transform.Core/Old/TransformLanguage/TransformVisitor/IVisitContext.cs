using CK.Transform.Core;

namespace CK.Transform.Core;

/// <summary>
/// This context is available on <see cref="TransformVisitor.VisitContext"/> property.
/// </summary>
public interface IVisitContext : IVisitContextBase
{
    /// <summary>
    /// Gets the visited node.
    /// </summary>
    AbstractNode VisitedNode { get; }

    /// <summary>
    /// Gets the range filter status for the <see cref="VisitedNode"/>.
    /// </summary>
    VisitedNodeRangeFilterStatus RangeFilterStatus { get; }

    /// <summary>
    /// Gets the current depth.
    /// </summary>
    int Depth { get; }

    /// <summary>
    /// Gets the current position.
    /// </summary>
    int Position { get; }

    /// <summary>
    /// Obtains the location of the currently visited node.
    /// When no nodes are being visited, the root is returned.
    /// </summary>
    /// <returns>A qualified location.</returns>
    NodeLocation GetCurrentLocation();

}
