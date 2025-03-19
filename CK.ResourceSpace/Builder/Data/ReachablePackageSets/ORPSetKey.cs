using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

/// <summary>
/// This beast is used for 2 things:
/// <list type="number">
///     <item>
///     It has a value semantics: this is used by the <see cref="ReachablePackageSetCacheBuilder"/> to implement a pool
///     of <see cref="RPSet"/>.
///     </item>
///     <item>
///     It is also used to compute optimal aggregation set of sets for a set.
///     </item>
/// </list>
/// This doesn't implement IEquatable because it is useless: it is used as an object key in the
/// dictionary of the builder.
/// </summary>
sealed class ORPSetKey : IRPSetKey
{
    readonly int[] _indexes;
    readonly int _hash;

    // "Real" RPSet constructor.
    public ORPSetKey( HashSet<ResPackage> set )
    {
        Throw.DebugAssert( set.Count >= 2 );
        int[] indexes = new int[set.Count];
        int i = 0;
        foreach( var e in set )
        {
            indexes[i++] = e.IsEventuallyLocalDependent ?  ~e.Index : e.Index;
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

    // From Mutable key.
    public ORPSetKey( MutableRPSetKey k )
    {
        _indexes = k.PackageIndexes.ToArray();
        Throw.DebugAssert( _indexes.Length >= 2 && _indexes.IsSortedStrict() );
        _hash = k.GetHashCode();
    }

    public void ResetMutable( MutableRPSetKey mutable ) => mutable.Reset( _indexes, 0, _indexes.Length );

    public int Length => _indexes.Length;

    public bool IsLocalDependent => _indexes[0] < 0;

    public ReadOnlySpan<int> PackageIndexes => _indexes.AsSpan();

    public override bool Equals( object? obj )
    {
        return obj is IRPSetKey k
                ? _indexes.AsSpan().SequenceEqual( k.PackageIndexes )
                : false;
    }

    public override int GetHashCode() => _hash;
}
