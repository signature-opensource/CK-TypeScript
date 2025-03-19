using CK.BinarySerialization;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Captures an reachable package set with only one package.
/// This only wraps the package's <see cref="RPEmpty"/> and behaves like a set with its item
/// but it doesn't really exist for us: it has the same index as the wrapped RPEmpty.
/// </summary>
[SerializationVersion( 0 )]
sealed class RPSingle : IReachablePackageSet, IRPRoot, ICKSlicedSerializable
{
    readonly RPEmpty _empty;

    public RPSingle( RPEmpty empty )
    {
        _empty = empty;
    }

    public int Index => _empty.Index;

    public bool IsLocalDependent => _empty.IsLocalDependent;

    public int Count => 1;

    public ResPackage RootPackage => _empty.RootPackage;

    public bool Contains( ResPackage item ) => _empty.RootPackage == item;

    public IEnumerator<ResPackage> GetEnumerator() => new CKEnumeratorMono<ResPackage>( _empty.RootPackage );

    public bool IsProperSubsetOf( IEnumerable<ResPackage> other )
    {
        Throw.CheckNotNullArgument( other );
        return other.Contains( _empty.RootPackage );
    }

    public bool IsProperSupersetOf( IEnumerable<ResPackage> other ) => false;

    public bool IsSubsetOf( IEnumerable<ResPackage> other )
    {
        Throw.CheckNotNullArgument( other );
        return other.Contains( _empty.RootPackage );
    }

    public bool IsSupersetOf( IEnumerable<ResPackage> other )
    {
        Throw.CheckNotNullArgument( other );
        using var e = other.GetEnumerator();
        if( !e.MoveNext() ) return true;
        do
        {
            if( e.Current != _empty.RootPackage ) return false;
        }
        while( e.MoveNext() );
        return true;
    }

    public bool Overlaps( IEnumerable<ResPackage> other )
    {
        Throw.CheckNotNullArgument( other );
        return other.Contains( _empty.RootPackage );
    }

    public bool SetEquals( IEnumerable<ResPackage> other )
    {
        Throw.CheckNotNullArgument( other );
        using var e = other.GetEnumerator();
        if( !e.MoveNext() ) return false;
        if( e.Current != _empty.RootPackage ) return false;
        return !e.MoveNext();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
