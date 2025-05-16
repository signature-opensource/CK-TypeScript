using CK.EmbeddedResources;
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
    // This array contains an entry for each IResPackageResources (stable or local => ResSpaceData.AllPackageResources).
    // Contains a List<TransformableItem> (stable) or the ILocalInput head of linked list (local).
    readonly object?[] _o;
    // This array contains an entry for local IResPackageResources (=> ResSpaceData.LocalPackageResources).
    // The HashSet contains a changed ILocalInput or the full path string of a new file candidate.
    readonly HashSet<object>?[] _localChanges;
    readonly ResSpaceData _spaceData;

    internal StableItemOrInputTracker( ResSpaceData spaceData )
    {
        _spaceData = spaceData;
        _o = new object?[_spaceData.AllPackageResources.Length];
        _localChanges = new HashSet<object>?[spaceData.LocalPackageResources.Length];
    }

    internal IEnumerable<TransformableItem> GetItems( ResPackage package, int languageIndex )
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

    // Currently called only when changed.FullPath ends with one of our TransformerHost's file extensions.
    // Always returns true except if the file is not in one of our local package resources.
    internal bool OnChange( IActivityMonitor monitor, PathChangedEvent changed )
    {
        var resources = changed.Resources;
        var o = _o[resources.Index];
        if( o is ILocalInput input )
        {
            Throw.DebugAssert( resources.LocalPath != null && resources.LocalIndex >= 0 );
            ref var changes = ref _localChanges[resources.LocalIndex];
            changes ??= new HashSet<object>();
            bool foundMatch = false;
            int lenRoot = resources.LocalPath.Length;
            for(; ; )
            {
                Throw.DebugAssert( input.Resources == resources && input.FullPath.Length >= lenRoot );
                if( changed.MatchSubPath( input.FullPath.AsSpan( lenRoot ) ) )
                {
                    foundMatch = true;
                    changes.Add( input );
                }
                var next = input.Next;
                if( next == null ) break;
                input = next;
            }
            if( !foundMatch )
            {
                // Adds the candidate in IResPackageResource slot.
                changes.Add( changed.FullPath );
            }
            return true;
        }
        if( o != null )
        {
            monitor.Warn( ActivityMonitor.Tags.ToBeInvestigated, $"Unexpected '{o.GetType():N}'." );
        }
        return false;
    }

    // Called only if at least one change has been recorded in _localchanges.
    internal HashSet<LocalItem>? ApplyChanges( IActivityMonitor monitor, TransformEnvironment environment )
    {
        // First pass: handles removal of ILocalInput that disappeared or didn't change.
        HashSet<NormalizedPath>? removedTargetPaths = null;
        List<ILocalInput>? removed = null;
        bool hasRemainingChanges = false;
        foreach( var set in _localChanges )
        {
            if( set != null && set.Count > 0 )
            {
                foreach( var c in set )
                {
                    if( c is ILocalInput input )
                    {
                        if( !input.InitializeApplyChanges( monitor, environment, ref removedTargetPaths ) )
                        {
                            removed ??= new List<ILocalInput>();
                            removed.Add( input );
                        }
                    }
                }
                if( removed != null && removed.Count > 0 )
                {
                    foreach( var i in removed )
                    {
                        set.Remove( i );
                    }
                    removed.Clear();
                }
                hasRemainingChanges |= set.Count > 0;
            }
        }
        if( !hasRemainingChanges )
        {
            return null;
        }
        var toBeInstalled = new HashSet<LocalItem>();
        // Second pass: Now that disappeared entities have been removed from the environment,
        //              we can update the changed ones and inject the new ones.
        //              This is done following the topological order and this is the key to
        //              handle this in a simple manner: resources of a IResPackageResource
        //              can safely handle the resources of the previous IResPackageResource
        //              as all their Reachables are settled.
        for( int iLocalResource = 0; iLocalResource < _localChanges.Length; iLocalResource++ )
        {
            HashSet<object>? set = _localChanges[iLocalResource];
            if( set != null && set.Count > 0 )
            {
                var resources = _spaceData.LocalPackageResources[iLocalResource];
                foreach( var c in set )
                {
                    if( c is string fullPath )
                    {
                        // New file. Language has alredy been (successfuly) obtained by OnChange but
                        // this may change in the future (if we need to handle folders), so it doesn't
                        // cost much to be defensive here.
                        var language = environment.TransformerHost.FindFromFilename( fullPath, out _ );
                        if( language != null )
                        {
                            // We reuse the Register of the initial phasis: it does everything
                            // we need and if it fails, we let the error be logged and continue.
                            var r = new ResourceLocator( resources.Resources, fullPath );
                            var text = ILocalInput.SafeReadText( monitor, r );
                            if( text != null )
                            {
                                var input = environment.Register( monitor, resources, language, r, text );
                                Throw.DebugAssert( input == null || input is ILocalInput );
                                if( input is LocalItem newItem )
                                {
                                    toBeInstalled.Add( newItem );
                                }
                            }
                        }
                    }
                    else
                    {
                        Throw.DebugAssert( c is ILocalInput );
                        ((ILocalInput)c).ApplyChanges( monitor, environment, toBeInstalled );
                    }
                }

            }
        }

        return toBeInstalled.Count > 0
                ? toBeInstalled
                : null;
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
        item.Prev = null;
        item.Next = null;
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
