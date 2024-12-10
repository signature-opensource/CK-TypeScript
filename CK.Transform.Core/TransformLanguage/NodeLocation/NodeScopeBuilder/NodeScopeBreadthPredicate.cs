using CK.Core;
using CK.Transform.Core;
using System;
using System.Diagnostics;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Builds scopes based on a node predicate. This is a breadth-first matcher: as soon as a node match,
/// none of its children will match.
/// </summary>
public sealed class NodeScopeBreadthPredicate : NodeScopeBuilder
{
    readonly Func<AbstractNode, bool> _predicate;
    readonly string _description;
    NodeLocationRange? _current;

    /// <summary>
    /// Initializes a new scope builder for scope that cover the top nodes that match a predicate.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <param name="predicateDescription">Should start with a verb like "have CK.sUserCreate full name" or "contain a select".</param>
    public NodeScopeBreadthPredicate( Func<AbstractNode, bool> predicate, string predicateDescription = "match a predicate" )
    {
        Throw.CheckNotNullArgument( predicate );
        _predicate = predicate;
        _description = $"(top-level nodes that {predicateDescription})";
    }

    NodeScopeBreadthPredicate( NodeScopeBreadthPredicate o )
    {
        _predicate = o._predicate;
        _description = o._description;
    }

    private protected override NodeScopeBuilder Clone() => new NodeScopeBreadthPredicate( this );

    private protected override void DoReset()
    {
        _current = null;
    }

    private protected override NodeLocationRange? DoEnter( IVisitContext context )
    {
        if( _current == null
            && context.RangeFilterStatus.IsIncludedInFilteredRange()
            && _predicate( context.VisitedNode ) )
        {
            var beg = context.GetCurrentLocation();
            Debug.Assert( beg.Node == context.VisitedNode );
            return _current = new NodeLocationRange( beg, context.LocationManager.GetRawLocation( beg.Position + context.VisitedNode.Width ) );
        }
        return null;
    }

    private protected override INodeLocationRange? DoLeave( IVisitContext context )
    {
        if( _current != null && _current.Beg.Node == context.VisitedNode )
        {
            _current = null;
        }
        return null;
    }

    private protected override INodeLocationRange? DoConclude( IVisitContextBase context )
    {
        return null;
    }

    /// <summary>
    /// Overridden to return the description of this predicate.
    /// </summary>
    /// <returns>A description.</returns>
    public override string ToString() => _description;

}


