using CK.Transform.Core;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Generalizes <see cref="SingleLocationFinder"/> and <see cref="MultiLocationFinder"/>.
/// </summary>
public interface INodeLocationFinderNode : IAbstractNode
{
    /// <summary>
    /// Gets the normalized cardinality.
    /// </summary>
    LocationCardinalityInfo GetCardinality();

    /// <summary>
    /// Gets the pattern to match.
    /// </summary>
    INodeLocationPatternNode Pattern { get; }
}
