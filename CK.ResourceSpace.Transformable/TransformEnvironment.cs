using CK.EmbeddedResources;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CK.Core;

sealed partial class TransformEnvironment
{
    readonly ResSpaceData _spaceData;
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
    // by reparsing the text (PostDeserialization clears thsi field to free this memory).
    // Stable FunctionSource are then only referenced by the TFunction they contain (local function sources
    // are also in the tracker).
    List<FunctionSource>? _functionSourceCollector;

    public TransformEnvironment( ResSpaceData spaceData, TransformerHost transformerHost )
    {
        _spaceData = spaceData;
        _transformerHost = transformerHost;
        _items = new Dictionary<NormalizedPath, TransformableItem>();
        _transformFunctions = new Dictionary<string, TFunction>();
        _tracker = new StableItemOrInputTracker( spaceData );
        _functionSourceCollector = new List<FunctionSource>();
    }

    internal IEnumerable<TransformableItem> Items => _items.Values;

    public TransformerHost TransformerHost => _transformerHost;

    internal Dictionary<string, TFunction> TransformFunctions => _transformFunctions;

    internal StableItemOrInputTracker Tracker => _tracker;

    internal bool Register( IActivityMonitor monitor, IResPackageResources resources, TransformerHost.Language language, ResourceLocator r )
    {
        var text = r.ReadAsText();
        if( language.TransformLanguage.IsTransformerLanguage )
        {
            if( resources.LocalPath != null )
            {
                var fSource = new LocalFunctionSource( resources, r.FullResourceName, text );
                if( !fSource.Initialize( monitor, this ) ) return false;
                _tracker.AddLocalInput( fSource );
                _functionSourceCollector?.Add( fSource );
            }
            else
            {
                FunctionSource fSource = new FunctionSource( resources, r.FullResourceName, text );
                if( !fSource.Initialize( monitor, this ) ) return false;
                _functionSourceCollector?.Add( fSource );
            }
        }
        else
        {
            var target = resources.Package.DefaultTargetPath.Combine( r.ResourceName.ToString() );
            if( _items.TryGetValue( target, out var exists ) )
            {
                monitor.Error( $"Both {r} and {exists.Origin} have the same targe final path '{target}'." );
                return false;
            }
            if( resources.LocalPath != null )
            {
                var item = new LocalItem( resources, r.FullResourceName, language.Index, target, text );
                _tracker.AddLocalInput( item );
            }
            else
            {
                var item = new TransformableItem( resources, r.FullResourceName, language.Index, target, text );
                _tracker.AddStableItem( item );
            }
        }
        return true;
    }

    internal ITransformable? FindTarget( IActivityMonitor monitor, FunctionSource source, TransformerFunction f )
    {
        GetTargetNameToFind( source, f, out var expectedPath, out var exactName, out var namePrefix );
        bool useNamePrefix = exactName.Length == 0;
        if( useNamePrefix && namePrefix.Length == 0 )
        {
            monitor.Error( $"""
                Unable to derive a target name for {source.Origin} transformer:
                {f.Text}
                """ );
            return null;
        }
        TransformableItem? best = null;
        List<TransformableItem>? ambiguities = null;
        foreach( var p in source.Resources.Reachables )
        {
            foreach( var candidate in _tracker.GetItems( p, f.Language.Index ) )
            {
                // This may be externalized in a strategy (configured by a TransformableFileHandler
                // constructor parameter).
                // Currently, this has been designed to:
                // - handle a "Less" transformer to find a ".css" or a ".less" item (exact file
                //   extensions are "erased" unless the transformer on "target" specifies an extension).
                // - handle a "top-bar.t" to locate a "top-bar.component.ts" item (thanks to the name prefix
                //   matching).
                // - Enables a transformer on "target" to disambiguate items thanks to the expectedPath.
                //   given 2 items "CK/Ng/AXIOSToken.ts" and "Partner/AXIOSToken.ts", a
                //   create typescript transformer on "Ng/AXIOSToken" will do the job.
                //   This may be enhanced ("CK/AXIOSToken.ts" - by the start, or by introducing globbing)
                //   if needed.
                //
                //   This doesn't handle "ambient hints" (and it seems not easy to honor them) like an
                //   ambient sql schema that will find a "CK.sUserRead.sql" item from a "sUserRead" target
                //   name. This is where an external (optional) strategy can handle this. 
                //
                var name = candidate.TargetPath.LastPart.AsSpan();
                if( MatchName( useNamePrefix, exactName, namePrefix, name ) )
                {
                    // Name matches, we must now handle the expectedPath.
                    if( expectedPath.Length == 0
                        || MatchExpectedPath( candidate.TargetPath.Path, expectedPath, name ) )
                    {
                        if( best == null )
                        {
                            best = candidate;
                        }
                        else
                        {
                            ambiguities ??= new List<TransformableItem>();
                            ambiguities.Add( candidate );
                        }
                    }
                }
            }
        }
        if( best == null || ambiguities != null )
        {
            using( monitor.OpenError( $"""
                            Unable to find the target for {source.Origin} transformer:
                            {f.Text}
                            """ ) )
            {
                var n = useNamePrefix ? $"NamePrefix: '{namePrefix}'" : $"ExactName: '{exactName}'";
                monitor.Error( $"Considering: {n}{(expectedPath.Length > 0 ? $", with expected path '{expectedPath}'" : "")}." );
                if( ambiguities != null )
                {
                    monitor.Error( $"""
                                Found {ambiguities.Count} ambiguous candidates:
                                {ambiguities.Select( i => i.TargetPath.Path ).Concatenate(Environment.NewLine)}
                                """ );
                            
                }
            }
            return null;
        }
        return best;

        static bool MatchName( bool useNamePrefix,
                               ReadOnlySpan<char> exactName,
                               ReadOnlySpan<char> namePrefix,
                               ReadOnlySpan<char> name )
        {
            if( useNamePrefix )
            {
                // There cannot be a name equals to namePrefix: the items are filtered by their
                // extensions: if they are her, their name has one of the extensions of the language.
                // So we only handle name longer than the prefix with an expected following '.'.
                if( name.Length > namePrefix.Length
                    && name[namePrefix.Length] == '.'
                    && name.StartsWith( namePrefix, StringComparison.Ordinal ) )
                {
                    return true;
                }
            }
            else if( name.Equals( exactName, StringComparison.Ordinal ) )
            {
                return true;
            }
            return false;
        }

        static bool MatchExpectedPath( string candidatePath, ReadOnlySpan<char> expectedPath, ReadOnlySpan<char> name )
        {
            // There may be no path at all if the item is mapped to the root.
            // We decrement the length by 1 to skip the latest / separator.
            var pathLen = candidatePath.Length - name.Length - 1;
            if( pathLen == expectedPath.Length
                  && expectedPath.Equals( candidatePath.AsSpan( 0, pathLen ), StringComparison.Ordinal ) )
            {
                return true;
            }
            if( pathLen > expectedPath.Length )
            {
                var cPath = candidatePath.AsSpan( 0, pathLen );
                if( cPath[pathLen-expectedPath.Length-1] == '/'
                    && cPath.EndsWith( expectedPath, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }

    static void GetTargetNameToFind( FunctionSource source,
                                     TransformerFunction f,
                                     out ReadOnlySpan<char> expectedPath,
                                     out ReadOnlySpan<char> exactName,
                                     out ReadOnlySpan<char> namePrefix )
    {
        var nameTofind = f.Target;
        if( string.IsNullOrWhiteSpace( nameTofind ) )
        {
            // No extension in this one. And no expected path.
            // The source name is not an exact name, only a prefix.
            expectedPath = default;
            exactName = default;
            namePrefix = source.SourceName;
            return;
        }
        // The function target may contain a sub path: it is an expected path.
        var n = nameTofind.AsSpan();
        int idx = n.LastIndexOf( '/' );
        if( idx >= 0 )
        {
            // Removes starting any /.
            expectedPath = n.Slice( 0, idx ).TrimStart( '/' );
            n = n.Slice( idx + 1 );
        }
        else
        {
            expectedPath = default;
        }
        // If the function target ends with an extension, it is an exactName,
        // otherwise we consider it as a namePrefix.
        var ext = f.Language.TransformLanguage.CheckLangageFilename( n );
        if( ext.Length > 0 )
        {
            exactName = n;
            namePrefix = default;
        }
        else
        {
            exactName = default;
            namePrefix = n;
        }
    }
}
