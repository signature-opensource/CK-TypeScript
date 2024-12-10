using CK.Transform.Core;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Exposes everything needed to manage location of nodes below a <see cref="Root"/>.
/// </summary>
public interface INodeLocationManager
{
    /// <summary>
    /// This can only be implemented in this assembly and <see cref="LocationRoot"/> does this
    /// with an explicit implementation to avoid poulluting its API.
    /// </summary>
    internal void ExternalImplementationsDisabled();

    /// <summary>
    /// Gets the root location.
    /// </summary>
    NodeLocation Root { get; }

    /// <summary>
    /// The location before this root. Its <see cref="SqlNodeLocation.Node"/> is <see cref="SqlKeyword.BegOfInput"/>
    /// and its <see cref="SqlNodeLocation.Position"/> is -1.
    /// </summary>
    NodeLocation BegMarker { get; }

    /// <summary>
    /// The location after this root: its <see cref="SqlNodeLocation.Node"/> is <see cref="SqlKeyword.EndOfInput"/> and 
    /// its <see cref="SqlNodeLocation.Position"/> is equal to the root node's width.
    /// </summary>
    NodeLocation EndMarker { get; }

    /// <summary>
    /// Gets a raw location object for the position.
    /// If a full location can efficiently be retrieved, the returned location may be a full one. 
    /// </summary>
    /// <param name="position">The position of the node.</param>
    /// <returns>A location.</returns>
    NodeLocation GetRawLocation( int position );

    /// <summary>
    /// Gets the full location for the position.
    /// </summary>
    /// <param name="position">The position of the node.</param>
    /// <returns>A full location.</returns>
    NodeLocation GetFullLocation( int position );

    /// <summary>
    /// Returns the qualified location of a node for the given position.
    /// The node must exist at this position otherwise an <see cref="ArgumentException"/> is thrown. 
    /// </summary>
    /// <param name="position">The position of the node.</param>
    /// <param name="node">The node that must exist at the given position.</param>
    /// <returns>The qualified location.</returns>
    NodeLocation GetQualifiedLocation( int position, AbstractNode node );
}
