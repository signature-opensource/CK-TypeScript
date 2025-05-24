using CK.EmbeddedResources;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CK.Core;

sealed partial class TransformEnvironment
{
    readonly TransformerHost _transformerHost;
    // Required to detect duplicate resources mapping to the same target path.
    // This is used during setup and live (when new files appear).
    readonly Dictionary<NormalizedPath, TransformableItem> _items;
    // All transformer functions are registered in this dictionary indexed by their name:
    // this detects homonyms and locates targets for transformers of transformer.
    readonly Dictionary<string, TFunction> _transformFunctions;
    // Registrar for stable TransformItem and ILocalInput: stable FunctionSource are
    // not tracked by this tracker. 
    readonly StableItemOrInputTracker _tracker;
    // FunctionSource collector is only used in PostDeserialization to restore the TransformerFunctions
    // by reparsing the text (PostDeserialization clears this field to free this memory).
    // Stable FunctionSource are then only referenced by the TFunction they contain (local function sources
    // are also in the tracker).
    // => This is null in Live.
    List<FunctionSource>? _functionSourceCollector;
    // Functions that lost their targets.
    // => This is not null only in Live.
    readonly HashSet<TFunction>? _unboundFunctions;

    internal TransformEnvironment( ResSpaceData spaceData, TransformerHost transformerHost )
    {
        _transformerHost = transformerHost;
        _items = new Dictionary<NormalizedPath, TransformableItem>();
        _transformFunctions = new Dictionary<string, TFunction>();
        _tracker = new StableItemOrInputTracker( spaceData );
        _functionSourceCollector = new List<FunctionSource>();
    }

    internal Dictionary<NormalizedPath,TransformableItem> Items => _items;

    internal TransformerHost TransformerHost => _transformerHost;

    internal Dictionary<string, TFunction> TransformFunctions => _transformFunctions;

    internal StableItemOrInputTracker Tracker => _tracker;

    [MemberNotNullWhen( true, nameof( UnboundFunctions ) )]
    internal bool IsLive => _unboundFunctions != null;

    internal HashSet<TFunction>? UnboundFunctions => _unboundFunctions;

    internal IResourceInput? Register( IActivityMonitor monitor,
                                       IResPackageResources resources,
                                       TransformerHost.Language language,
                                       ResourceLocator r,
                                       string? loadedText = null )
    {
        // In Live, we use the ILocalInput to ensure that we can load the text before
        // registering the resource.
        Throw.DebugAssert( !IsLive || loadedText != null );
        var text = loadedText ?? r.ReadAsText();
        if( language.IsTransformLanguage )
        {
            if( resources.LocalPath != null )
            {
                var fSource = new LocalFunctionSource( resources, r.FullResourceName, text );
                if( !fSource.Initialize( monitor, this ) ) return null;
                _tracker.AddLocalInput( fSource );
                _functionSourceCollector?.Add( fSource );
                return fSource;
            }
            FunctionSource fLocalSource = new FunctionSource( resources, r.FullResourceName, text );
            if( !fLocalSource.Initialize( monitor, this ) ) return null;
            _functionSourceCollector?.Add( fLocalSource );
            return fLocalSource;
        }

        var targetPath = resources.Package.DefaultTargetPath.Combine( r.ResourceName.ToString() );
        if( _items.TryGetValue( targetPath, out var exists ) )
        {
            monitor.Error( $"""
                Both:
                - {r}
                - and {exists.Origin}
                have the same target final path '{targetPath}'.
                """ );
            return null;
        }

        var item = CreateItem( _tracker, resources, language, r, text, targetPath );
        _items.Add( targetPath, item );
        return item;

        static TransformableItem CreateItem( StableItemOrInputTracker tracker,
                                             IResPackageResources resources,
                                             TransformerHost.Language language,
                                             ResourceLocator r,
                                             string text,
                                             NormalizedPath target )
        {
            if( resources.LocalPath != null )
            {
                var item = new LocalItem( resources, r.FullResourceName, text, language.Index, target );
                tracker.AddLocalInput( item );
                return item;
            }
            var stableItem = new TransformableItem( resources, r.FullResourceName, text, language.Index, target );
            tracker.AddStableItem( stableItem );
            return stableItem;
        }
    }

    internal ITransformable? FindTarget( IActivityMonitor monitor, FunctionSource source, TransformerFunction f )
    {
        return f.Language.IsTransformLanguage
                    ? FindFunctionTarget( monitor, source, f )
                    : FindTransformableItemTarget( monitor, source, f );
    }

    internal TransformerHost.Language? FindFromFileName( ReadOnlySpan<char> fileName, out ReadOnlySpan<char> extension )
    {
        return _transformerHost.FindFromFileName( fileName, out extension );
    }

    internal int Rebind( IActivityMonitor monitor, ITransformable newItem )
    {
        Throw.DebugAssert( IsLive );
        var toRebind = UnboundFunctions.Where( u => u.Target.TransfomableTargetName == newItem.TransfomableTargetName )
                                       .OrderBy( f => f.Source.Resources.Index )
                                       .ToList();
        TFunction? previous = null;
        foreach( var f in toRebind )
        {
            newItem.Add( f, previous );
            f.SetNewTarget( newItem );
            previous = f;
            UnboundFunctions.Remove( f );
        }
        return toRebind.Count;
    }
}
