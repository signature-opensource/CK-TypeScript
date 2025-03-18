using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

/// <summary>
/// This beast is used for 2 things:
/// <list type="number">
///     <item>
///     It has a value semantics: this is used by the ReachablePackageSetBuilder to implement a pool.
///     </item>
///     <item>
///     It is also used to compute optimal aggregation set of sets for a set.
///     </item>
/// </list>
/// </summary>
sealed class ReachablePackageSetKey : IEquatable<ReachablePackageSetKey>
{
    readonly int[] _indexes;
    readonly int _hash;

    public ReachablePackageSetKey( HashSet<ResPackage> set )
    {
        int[] indexes = new int[set.Count];
        int i = 0;
        foreach( var e in set )
        {
            indexes[i++] = e.Index;
        }
        Array.Sort( indexes );
        HashCode c = new HashCode();
        for( i = 0; i < indexes.Length; ++i )
        {
            c.Add( indexes[i] );
        }
        _indexes = indexes;
        _hash = c.ToHashCode();
    }

    public bool Equals( ReachablePackageSetKey? other ) => other != null && _indexes.AsSpan().SequenceEqual( other._indexes.AsSpan() );

    public override bool Equals( object? obj ) => Equals( obj as ReachablePackageSetKey );

    public override int GetHashCode()
    {
        Throw.DebugAssert( _indexes != null );
        return _hash;
    }
}
