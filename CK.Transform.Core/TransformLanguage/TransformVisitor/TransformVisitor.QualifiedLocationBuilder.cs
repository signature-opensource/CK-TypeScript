using CK.Core;
using CK.Transform.Core;
using System.Collections.Generic;

namespace CK.Transform.TransformLanguage;

public partial class TransformVisitor
{
    /// <summary>
    /// Creates current locations during traversal that have associated <see cref="SqlNodeLocation.Node"/>.
    /// </summary>
    sealed class QualifiedLocationBuilder
    {
        struct PLoc
        {
            public readonly NodeLocation? Loc;
            public readonly AbstractNode Node;
            public readonly int Pos;
            public readonly int CurChildPos;

            public PLoc( NodeLocation? loc, AbstractNode n, int idx, int curChildPos )
            {
                Loc = loc;
                Node = n;
                Pos = idx;
                CurChildPos = curChildPos;
            }
        }
        LocationRoot? _root;
        readonly List<PLoc> _path;

        internal QualifiedLocationBuilder()
        {
            _path = new List<PLoc>();
        }

        /// <summary>
        /// Gets the current location root.
        /// </summary>
        internal LocationRoot? Root => _root;

        /// <summary>
        /// Gets the current visit depth.
        /// </summary>
        internal int Depth => _path.Count - 1;

        /// <summary>
        /// Gets the current visit position.
        /// </summary>
        internal int Position => _path[_path.Count - 1].CurChildPos;

        /// <summary>
        /// Resets the builder on a <see cref="Root"/>.
        /// </summary>
        /// <param name="root">The new root.</param>
        internal void Reset( LocationRoot root )
        {
            Throw.DebugAssert( root != null );
            _root = root;
            _path.Clear();
        }

        /// <summary>
        /// Called before visiting a node.
        /// </summary>
        /// <param name="n">The node to be visited.</param>
        internal void Enter( AbstractNode n )
        {
            Throw.DebugAssert( _root != null );
            if( n == _root.Node ) _path.Add( new PLoc( _root, n, 0, 0 ) );
            else
            {
                PLoc c = _path[_path.Count - 1];
                _path.Add( new PLoc( null, n, c.CurChildPos, c.CurChildPos ) );
            }
        }

        /// <summary>
        /// Called after having visited a node.
        /// </summary>
        /// <param name="n">The visited node.</param>
        public void Leave( AbstractNode n )
        {
            Throw.DebugAssert( _root != null );
            Throw.DebugAssert( _path[_path.Count - 1].Node == n );
            int top = _path.Count;
            _path.RemoveAt( --top );
            if( n != _root.Node )
            {
                PLoc c = _path[--top];
                _path[top] = new PLoc( c.Loc, c.Node, c.Pos, c.CurChildPos + n.Width );
            }
        }

        /// <summary>
        /// Obtains the location of the currently visited node.
        /// When no nodes are beeing visited, <see cref="Root"/> is returned.
        /// </summary>
        /// <returns>A qualified location.</returns>
        public NodeLocation GetCurrent()
        {
            Throw.DebugAssert( _root != null );
            NodeLocation prev = _root;
            for( int i = 1; i < _path.Count; i++ )
            {
                PLoc c = _path[i];
                NodeLocation? loc = c.Loc;
                if( loc == null )
                {
                    loc = _root.EnsureLocation( prev, c.Node, c.Pos );
                    _path[i] = new PLoc( loc, c.Node, c.Pos, c.CurChildPos );
                }
                prev = loc;
            }
            return prev;
        }

    }

}
