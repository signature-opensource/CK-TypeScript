using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

/// <summary>
/// Internal specialization of a <see cref="NodeLocation"/> that is bound to a root node
/// and holds the cache of other <see cref="NodeLocation"/> in this node.
/// </summary>
sealed class LocationRoot : NodeLocation, INodeLocationManager
{
    void INodeLocationManager.ExternalImplementationsDisabled() { }

    readonly Dictionary<AbstractNode, NodeLocation> _qualifiedCache;
    readonly NodeLocation[]? _fullCache;

    public NodeLocation BegMarker { get; }

    public NodeLocation EndMarker { get; }

    public LocationRoot( AbstractNode node, bool cacheFullLocations = true )
        : base( node )
    {
        Throw.CheckNotNullArgument( node );
        BegMarker = new NodeLocation( this, NodeLocation.BegOfInput, -1 );
        EndMarker = new NodeLocation( this, NodeLocation.EndOfInput, node.Width );
        if( cacheFullLocations ) _fullCache = new NodeLocation[node.Width];
        _qualifiedCache = new Dictionary<AbstractNode, NodeLocation>();
    }

    internal NodeLocation GetLastLocation( bool fullLocation ) => fullLocation
                                                                    ? GetFullLocation( Node.Width - 1 )
                                                                    : GetRawLocation( Node.Width - 1 );

    public NodeLocation GetRawLocation( int position )
    {
        if( position < 0 ) return BegMarker;
        if( position >= Node.Width ) return EndMarker;
        if( position == 0 ) return this;
        return new NodeLocation( this, null, position );
    }

    public NodeLocation GetFullLocation( int position )
    {
        if( position < 0 ) return BegMarker;
        if( position >= Node.Width ) return EndMarker;
        if( _fullCache != null )
        {
            return _fullCache[position] ?? (_fullCache[position] = ComputeFullLocation( position ));
        }
        return ComputeFullLocation( position );
    }

    public NodeLocation GetQualifiedLocation( int position, AbstractNode node )
    {
        NodeLocation? loc;
        if( node == Node ) return this;
        if( _qualifiedCache.TryGetValue( node, out loc ) ) return loc;
        loc = GetFullLocation( position );
        if( loc != null )
        {
            if( loc.Node == node ) return loc;
            loc = loc.Parent;
            if( node.Width == 0 )
            {
                loc = new NodeLocation( loc, node, position );
                _qualifiedCache.Add( node, loc );
                return loc;
            }
            do
            {
                if( loc.Node == node ) return loc;
                loc = loc.Parent;
                Throw.DebugAssert( "Already cached.", loc == null || _qualifiedCache.ContainsKey( loc.Node ) );
            }
            while( loc != null );
        }
        throw new ArgumentException( "Node does not exist at this position." );
    }

    NodeLocation ComputeFullLocation( int position )
    {
        Throw.DebugAssert( position >= 0 && position < Node.Width );
        NodeLocation loc = this;
        var token = Node.LocateToken( position, ( n, p ) =>
        {
            NodeLocation newLoc;
            if( !_qualifiedCache.TryGetValue( n, out newLoc ) )
            {
                _qualifiedCache.Add( n, (newLoc = new NodeLocation( loc, n, p )) );
            }
            loc = newLoc;
        } );
        Throw.DebugAssert( token != Node || loc == this );
        return loc == this && token == Node
                ? loc
                : new NodeLocation( loc, token, position );
    }

    internal NodeLocation EnsureLocation( NodeLocation parent, AbstractNode node, int pos )
    {
        if( node is TokenNode )
        {
            if( _fullCache != null )
            {
                return _fullCache[pos] ?? (_fullCache[pos] = new NodeLocation( parent, node, pos ));
            }
        }
        else
        {
            if( !_qualifiedCache.TryGetValue( node, out var loc ) )
            {
                _qualifiedCache.Add( node, (loc = new NodeLocation( parent, node, pos )) );
            }
            return loc;
        }
        return new NodeLocation( parent, node, pos );
    }
}

