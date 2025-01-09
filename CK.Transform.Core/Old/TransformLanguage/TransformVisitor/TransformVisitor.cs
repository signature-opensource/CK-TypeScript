using CK.Core;
using CK.Transform.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CK.Transform.Core;

/// <summary>
/// Extends <see cref="BaseNodeVisitor"/> to support <see cref="NodeLocation"/> handling
/// and <see cref="StopVisit"/> capability.
/// </summary>
public partial class TransformVisitor : BaseNodeVisitor
{
    sealed class VisitorContext : IVisitContext
    {
        readonly QualifiedLocationBuilder _builder;
        INodeLocationRange? _rangeFilter;
        IEnumerator<NodeLocationRange>? _filteredRange;
        int _overridePos;
        VisitedNodeRangeFilterStatus _rangeFilterStatus;

        public VisitorContext( IActivityMonitor monitor )
        {
            Monitor = monitor;
            _builder = new QualifiedLocationBuilder();
        }

        public IActivityMonitor Monitor { get; }

        public void Reset( LocationRoot root, INodeLocationRange? rangeFilter )
        {
            _builder.Reset( root );
            Debug.Assert( _builder.Depth == -1 );
            _filteredRange = null;
            if( (_rangeFilter = rangeFilter) != null )
            {
                var e = rangeFilter.MergeContiguous().GetEnumerator();
                if( e.MoveNext() ) _filteredRange = e;
            }
            _rangeFilterStatus = VisitedNodeRangeFilterStatus.None;
        }

        public void EnsureRootForNode( AbstractNode root )
        {
            if( root != _builder.Root?.Node ) _builder.Reset( new LocationRoot( root ) );
        }

        public INodeLocationRange RangeFilter => _rangeFilter;

        public VisitedNodeRangeFilterStatus RangeFilterStatus
        {
            get { return _rangeFilterStatus; }
            set { _rangeFilterStatus = value; }
        }

        public VisitedNodeRangeFilterStatus Enter( AbstractNode prev, AbstractNode n )
        {
            _rangeFilterStatus = VisitedNodeRangeFilterStatus.None;
            Tag = null;
            VisitedNode = n;
            _builder.Enter( n );
            int p = _builder.Position;

            if( _rangeFilter == null )
            {
                _rangeFilterStatus = p == 0
                                        ? VisitedNodeRangeFilterStatus.FIntersecting
                                        : VisitedNodeRangeFilterStatus.FIntersecting | VisitedNodeRangeFilterStatus.FBegAfter;
                if( p < _builder.Root.Node.Width - 1 ) _rangeFilterStatus |= VisitedNodeRangeFilterStatus.FEndBefore;
            }
            else
            {
                int endPos;
                if( _filteredRange == null || (endPos = p + n.Width) <= _filteredRange.Current.Beg.Position )
                {
                    Leave( prev );
                }
                else
                {
                    _rangeFilterStatus |= VisitedNodeRangeFilterStatus.FIntersecting;
                    int deltaBeg = _builder.Position - _filteredRange.Current.Beg.Position;
                    if( deltaBeg < 0 ) _rangeFilterStatus |= VisitedNodeRangeFilterStatus.FBegBefore;
                    else if( deltaBeg > 0 ) _rangeFilterStatus |= VisitedNodeRangeFilterStatus.FBegAfter;
                    int deltaEnd = endPos - _filteredRange.Current.End.Position;
                    if( deltaEnd < 0 ) _rangeFilterStatus |= VisitedNodeRangeFilterStatus.FEndBefore;
                    else if( deltaEnd > 0 ) _rangeFilterStatus |= VisitedNodeRangeFilterStatus.FEndAfter;
                }
            }
            return _rangeFilterStatus;
        }

        public void Leave( AbstractNode prev )
        {
            _builder.Leave( VisitedNode );
            VisitedNode = prev;
            if( prev != null && _filteredRange != null )
            {
                int p = _builder.Position;
                while( p >= _filteredRange.Current.End.Position )
                {
                    if( !_filteredRange.MoveNext() )
                    {
                        _filteredRange = null;
                        break;
                    }
                }
            }
        }

        public INodeLocationManager LocationManager => _builder.Root;

        public AbstractNode VisitedNode { get; private set; }

        public object Tag { get; set; }

        public LocationRoot Root => _builder.Root;

        public int Depth => _builder.Depth;

        public int Position => _overridePos >= 0 ? _overridePos : _builder.Position;

        public void OverridePosition( int pos = -1 )
        {
            _overridePos = pos;
        }

        public NodeLocation GetCurrentLocation() => _builder.GetCurrent();

    }

    readonly VisitorContext _context;
    bool _hasUnParsedText;
    bool _stop;

    /// <summary>
    /// Initializes a new location visitor.
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    protected TransformVisitor( IActivityMonitor monitor )
    {
        Throw.CheckNotNullArgument( monitor );
        _context = new VisitorContext( monitor );
    }

    /// <summary>
    /// Gets the monitor associated to this visitor.
    /// </summary>
    public IActivityMonitor Monitor => _context.Monitor;

    /// <summary>
    /// Overridden to adapt this public inherited method to the internals of this implementation.
    /// This visits the root without any range filter.
    /// </summary>
    /// <param name="root">The root node to visit.</param>
    /// <returns>The visited result.</returns>
    public override sealed AbstractNode? VisitRoot( IAbstractNode root ) => VisitRoot( root, rangeFilter: null );

    /// <summary>
    /// Visits the provided <paramref name="root"/>, applying the <paramref name="rangeFilter"/> if any.
    /// </summary>
    /// <param name="root">The node to visit.</param>
    /// <param name="rangeFilter">Optional range to consider.</param>
    /// <returns>The result of the visit.</returns>
    public AbstractNode? VisitRoot( IAbstractNode root, INodeLocationRange? rangeFilter )
    {
        Throw.CheckNotNullArgument( root );
        _context.EnsureRootForNode( Unsafe.As<AbstractNode>( root ) );
        return VisitRoot( _context.Root, rangeFilter );
    }

    internal AbstractNode? VisitRoot( LocationRoot root, INodeLocationRange? rangeFilter )
    {
        Debug.Assert( root != null && root.Node != null );
        if( rangeFilter == NodeLocationRange.EmptySet ) return root.Node;
        _hasUnParsedText = false;
        _context.Reset( root, rangeFilter );
        return base.VisitRoot( root.Node );
    }

    /// <summary>
    /// Overridden to update <see cref="VisitContext"/> and check scope. If the node is in the scope,
    /// calls <see cref="BeforeVisitItem"/>, call the visit itself (base method), call <see cref="AfterVisitItem"/> 
    /// and restore VisitContext.
    /// </summary>
    /// <param name="e">The node to visit.</param>
    /// <returns>The visited result node.</returns>
    protected override AbstractNode? VisitItem( IAbstractNode e )
    {
        AbstractNode? v = Unsafe.As<AbstractNode>( e );
        var prev = _context.VisitedNode;
        VisitedNodeRangeFilterStatus status = _context.Enter( prev, v );
        if( status != 0 )
        {
            // We use the stack here to restore the position, the status and the Tag of the visited
            // item before calling AfterVisitItem: this enables the location builder
            // to not use a stack (the LigthLocationBuilder does not use a stack).
            int savePos = _context.Position;
            bool doChildrenVisit = BeforeVisitItem() && !_stop;
            object tag = _context.Tag;
            if( doChildrenVisit ) v = base.VisitItem( e );
            // Restores the item position by overriding it.
            _context.OverridePosition( savePos );
            _context.Tag = tag;
            _context.RangeFilterStatus = status;
            if( v != null )
            {
                v = AfterVisitItem( v );
            }
            // Clears the override.
            _context.OverridePosition();
            _context.Leave( prev );
        }
        return v;
    }

    /// <summary>
    /// Gets whether unparsed text has been injected during any previous transformation.
    /// </summary>
    public bool HasUnParsedText => _hasUnParsedText;

    /// <summary>
    /// Called by <see cref="VisitItem"/> before the visit. 
    /// The <see cref="VisitContext"/> is bound to the node that will be visited.
    /// </summary>
    /// <returns>
    /// True (the default) to visit the children. False to skip the visit of the current node. 
    /// </returns>
    protected virtual bool BeforeVisitItem() => true;

    /// <summary>
    /// Called by <see cref="VisitItem(IAbstractNode)"/> after the visit.
    /// The <see cref="VisitContext"/> is bound to the node that has been visited.
    /// </summary>
    /// <param name="visitResult">
    /// The visited node (same as <see cref="VisitContext"/>.VisitedNode if no mutation occurred).
    /// </param>
    /// <returns>The visitResult node.</returns>
    protected virtual AbstractNode? AfterVisitItem( AbstractNode visitResult ) => visitResult;

    /// <summary>
    /// Calling this method stops the visit.
    /// </summary>
    /// <param name="hasUnParsedText">Optionally sets <see cref="HasUnParsedText"/> to true.</param>
    protected void StopVisit( bool hasUnParsedText = false )
    {
        _hasUnParsedText |= hasUnParsedText;
        _stop = true;
    }

    /// <summary>
    /// Sets <see cref="HasUnParsedText"/> to true.
    /// </summary>
    protected void SetHasUnParsedText() => _hasUnParsedText = true;

    /// <summary>
    /// Gets whether <see cref="StopVisit"/> has been called.
    /// </summary>
    protected bool IsStoppedVisit => _stop;

    /// <summary>
    /// Gets the current visit context. 
    /// </summary>
    protected IVisitContext VisitContext => _context;

}
