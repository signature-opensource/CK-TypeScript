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
    // Required for Live: this is used to map a file change (the resource)
    // to the source to shoot (until it revives or dies).
    readonly Dictionary<ResourceLocator, TransformableSource> _sources;
    // Required to detect duplicate resources mapping to the same target path.
    readonly Dictionary<NormalizedPath, TItem> _items;
    // Items are registered in a double linked list per origin package: this is
    // used to find the transformable targets with the help of ResPackage.Reachables
    // and AfterReachables. There's unfortunately no real way to locate a target by its
    // name without a O(n) search because we want to allow "short names" to be used and
    // not impose full name targets. Moreover, this is only used to locate transformer targets,
    // not all items. So we privilegiate add/remove efficiency.
    readonly TItem?[] _packageItemHead;
    // All transformer functions are registered in this dictionary indexed by their name:
    // this detects homonyms.
    readonly Dictionary<string, TFunction> _transformFunctions;

    public TransformEnvironment( ResSpaceData spaceData, TransformerHost transformerHost )
    {
        _spaceData = spaceData;
        _transformerHost = transformerHost;
        _sources = new Dictionary<ResourceLocator, TransformableSource>();
        _items = new Dictionary<NormalizedPath, TItem>();
        // There should not be Items in AppResources (only transformers) but if there are,
        // we don't track them (hence the -1).
        _packageItemHead = new TItem?[spaceData.Packages.Length-1];
        _transformFunctions = new Dictionary<string, TFunction>();
    }

    internal IEnumerable<TItem> Items => _items.Values;

    public TransformerHost TransformerHost => _transformerHost;

    internal Dictionary<string, TFunction> TransformFunctions => _transformFunctions;

    internal Dictionary<ResourceLocator, TransformableSource> Sources => _sources;

    internal bool Register( IActivityMonitor monitor, IResPackageResources resources, ResourceLocator r )
    {
        var language = _transformerHost.FindFromFilename( r.Name, out _ );
        if( language != null )
        {
            if( _sources.ContainsKey( r ) )
            {
                monitor.Error( $"{r} is already registered." );
                return false;
            }
            var text = r.ReadAsText();
            if( language.TransformLanguage.IsTransformerLanguage )
            {
                var fSource = new TFunctionSource( resources, r, text );
                if( fSource.Initialize( monitor, this ) )
                {
                    _sources.Add( r, fSource );
                    return true;
                }
                return false;
            }
            else
            {
                var target = resources.Package.DefaultTargetPath.Combine( r.ResourceName.ToString() );
                return AddItem( monitor, resources, r, language, target, text );
            }
        }
        return true;
    }

    bool AddItem( IActivityMonitor monitor,
                  IResPackageResources resources,
                  ResourceLocator r,
                  TransformerHost.Language language,
                  NormalizedPath target,
                  string text )
    {
        if( _items.TryGetValue( target, out var exists ) )
        {
            monitor.Error( $"Both {r} and {exists.Origin} have the same targe final path '{target}'." );
            return false;
        }
        var item = new TItem( resources, r, language, target, text );
        if( !resources.IsAppResources )
        {
            AddPackageItem( item );
        }
        _items.Add( target, item );
        return true;
    }

    internal ITransformable? FindTarget( IActivityMonitor monitor, TFunctionSource source, TransformerFunction f )
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
        TItem? best = null;
        List<TItem>? ambiguities = null;
        foreach( var p in source.Resources.Reachables )
        {
            var candidate = _packageItemHead[p.Index];
            while( candidate != null )
            {
                if( candidate.Language == f.Language )
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
                                ambiguities ??= new List<TItem>();
                                ambiguities.Add( candidate );
                            }
                        }
                    }

                }
                candidate = candidate._nextInPackage;
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

    static void GetTargetNameToFind( TFunctionSource source,
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

    void AddPackageItem( TItem item )
    {
        Throw.DebugAssert( !item.Resources.IsAppResources );
        ref var first = ref _packageItemHead[item.Resources.Package.Index];
        if( first != null )
        {
            Throw.DebugAssert( first._prevInPackage == null );
            first._prevInPackage = item;
            item._nextInPackage = first;
        }
        first = item;
        CheckPackageItemInvariant();
    }

    void RemovePackageItem( TItem item )
    {
        var prev = item._prevInPackage;
        if( prev != null )
        {
            Throw.DebugAssert( prev._nextInPackage == item );
            prev._nextInPackage = item._prevInPackage;
        }
        else
        {
            ref var first = ref _packageItemHead[item.Resources.Package.Index];
            Throw.DebugAssert( first == item );
            first = item;
        }
        var next = item._nextInPackage;
        if( next != null )
        {
            next._prevInPackage = prev;
        }
        CheckPackageItemInvariant();
    }

    [Conditional("DEBUG")]
    void CheckPackageItemInvariant()
    {
        for( int i = 0; i < _packageItemHead.Length; i++ )
        {
            TItem? item = _packageItemHead[i];
            if( item != null )
            {
                Throw.DebugAssert( item._prevInPackage == null );
                var expectedPackage = _spaceData.Packages[i];
                do
                {
                    Throw.DebugAssert( item.Resources.Package == expectedPackage );
                    var next = item._nextInPackage;
                    if( next != null )
                    {
                        Throw.DebugAssert( next._prevInPackage == item );
                    }
                    item = next;
                }
                while( item != null );
            }
        }
    }
}
