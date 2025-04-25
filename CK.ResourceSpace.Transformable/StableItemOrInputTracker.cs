using CK.BinarySerialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Tracks the stable <see cref="TransformableItem"/> and the <see cref="ILocalInput"/> (that
/// are the local items and the <see cref="LocalFunctionSource"/>) indexed by the <see cref="IResPackageResources"/>.
/// <para>
/// Stable items are registered in a list (deserialized in live context as an array).
/// Local inputs are registered in a double linked list per origin package resources. This is used
/// to locate the changed resources.
/// </para>
/// <para>
/// For items, these buckets are used to find the transformable targets (when the transformer is not
/// a transfomer of transformer) with the help of ResPackage.Reachables and AfterReachables.
/// There's unfortunately no real way to locate a target by its name without a O(n) search because
/// we want to allow "short names" to be used and not impose full name targets: building a "smart" index
/// of names would be costly...
/// Moreover, this is only used to locate transformer targets, not all items.
/// </para>
/// </summary>
sealed partial class StableItemOrInputTracker
{
    // This array contains an entry for each IResPackageResources (stable or local).
    readonly object?[] _o;
    readonly ResSpaceData _spaceData;

    public StableItemOrInputTracker( ResSpaceData spaceData )
    {
        _spaceData = spaceData;
        _o = new object?[_spaceData.AllPackageResources.Length];
    }

    public IEnumerable<TransformableItem> GetItems( ResPackage package, int languageIndex )
    {
        return GetItems( package.Resources, languageIndex ).Concat( GetItems( package.AfterResources, languageIndex ) );
    }

    IEnumerable<TransformableItem> GetItems( IResPackageResources resources, int languageIndex )
    {
        return _o[resources.Index] switch
        {
            ILocalInput first => GetItems( first, languageIndex ),
            IReadOnlyList<TransformableItem> items => items.Where( i => i.LanguageIndex == languageIndex ),
            _ => Array.Empty<TransformableItem>()
        };

        static IEnumerable<TransformableItem> GetItems( ILocalInput first, int languageIndex )
        {
            ILocalInput? i = first;
            do
            {
                if( i is TransformableItem item && item.LanguageIndex == languageIndex )
                {
                    yield return item;
                }
                i = i.Next;
            }
            while( i != null );
        }
    }

    public void OnChange( IActivityMonitor monitor,
                          TransformEnvironment environment,
                          PathChangedEvent changed )
    {
        var resources = changed.Resources;
        var o = _o[resources.Index];
        if( o is ILocalInput input )
        {
            Throw.DebugAssert( resources.LocalPath != null );
            bool foundMatch = false;
            int lenRoot = resources.LocalPath.Length;
            for(; ; )
            {
                Throw.DebugAssert( input.Resources == resources && input.FullPath.Length >= lenRoot );
                if( changed.MatchSubPath( input.FullPath.AsSpan( lenRoot ) ) )
                {
                    foundMatch = true;
                    input.OnChange( monitor, environment );
                }
                var next = input.Next;
                if( next == null ) break;
                input = next;
            }
            if( !foundMatch )
            {
                // TODO: inject the candidate in IResPackageResource slot... But how?
            }
        }
        if( o != null )
        {
            monitor.Warn( ActivityMonitor.Tags.ToBeInvestigated, $"Unexpected '{o.GetType():N}'." );
        }
    }

    public void AddStableItem( TransformableItem stable )
    {
        Throw.DebugAssert( stable is not LocalItem );
        ref var o = ref _o[stable.Resources.Index];
        Throw.DebugAssert( o == null || o is List<TransformableItem> );
        if( o == null )
        {
            o ??= new List<TransformableItem>() { stable };
        }
        else
        {
            ((List<TransformableItem>)o).Add( stable );
        }
    }

    public void AddLocalInput( ILocalInput input )
    {
        CheckInvariant();
        ref var o = ref _o[input.Resources.Index];
        Throw.DebugAssert( o == null || o is ILocalInput );
        if( o != null )
        {
            var first = (ILocalInput)o;
            Throw.DebugAssert( first.Prev == null );
            first.Prev = input;
            input.Next = first;
        }
        o = input;
        CheckInvariant();
    }

    public void Remove( ILocalInput item )
    {
        CheckInvariant();
        var prev = item.Prev;
        var next = item.Next;
        if( prev != null )
        {
            Throw.DebugAssert( prev.Next == item );
            prev.Next = next;
        }
        else
        {
            ref var o = ref _o[item.Resources.Package.Index];
            Throw.DebugAssert( o == item );
            o = next;
        }
        if( next != null )
        {
            next.Prev = prev;
        }
        CheckInvariant();
    }

    [Conditional( "DEBUG" )]
    void CheckInvariant()
    {
        for( int i = 0; i < _o.Length; i++ )
        {
            var expectedResources = _spaceData.AllPackageResources[i];
            var o = _o[i];
            if( o is ILocalInput input )
            {
                Throw.DebugAssert( input.Prev == null );
                ILocalInput? item = input;
                do
                {
                    Throw.DebugAssert( item.Resources == expectedResources );
                    var next = item.Next;
                    if( next != null )
                    {
                        Throw.DebugAssert( next.Prev == item );
                    }
                    item = next;
                }
                while( item != null );
            }
            else if( o is IReadOnlyList<TransformableItem> items )
            {
                foreach( var item in items )
                {
                    Throw.DebugAssert( item.Resources == expectedResources );
                }
            }
            else if( o != null )
            {
                Throw.Exception( "Expected IReadOnlyList<TransformableItem> or ILocalInput or null." );
            }
        }
    }

}
