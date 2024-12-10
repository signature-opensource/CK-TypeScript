using System;
using System.Collections.Generic;

namespace CK.Transform.TransformLanguage;

public static class NodelocationRangeExtensions
{
    public static INodeLocationRange Intersect( this INodeLocationRange @this, INodeLocationRange other )
    {
        if( other == null ) throw new ArgumentNullException( nameof( other ) );
        return NodeScopeIntersect.DoIntersect( @this, other );
    }

    public static INodeLocationRange Union( this INodeLocationRange @this, INodeLocationRange other )
    {
        if( other == null ) throw new ArgumentNullException( nameof( other ) );
        return NodeScopeUnion.DoUnion( @this, other );
    }

    public static INodeLocationRange Except( this INodeLocationRange @this, INodeLocationRange other )
    {
        if( other == null ) throw new ArgumentNullException( nameof( other ) );
        return NodeScopeExcept.DoExcept( @this, other );
    }

    public static IEnumerable<NodeLocationRange> MergeContiguous( this IEnumerable<NodeLocationRange> @this )
    {
        using( var e = @this.GetEnumerator() )
        {
            bool hasNext = e.MoveNext();
            while( hasNext )
            {
                NodeLocationRange current = e.Current;
                hasNext = e.MoveNext();
                while( hasNext && current.End.Position == e.Current.Beg.Position )
                {
                    current = current.InternalSetEnd( e.Current.End );
                    hasNext = e.MoveNext();
                }
                yield return current;
            }
        }
    }

}

