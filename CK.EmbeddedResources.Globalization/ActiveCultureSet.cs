using CK.Core;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CK.Core;

/// <summary>
/// Helper that captures a compact tree of <see cref="ActiveCulture"/>.
/// All parent cultures are guaranteed to exist.
/// <para>
/// This is a read only dictionary of <see cref="NormalizedCultureInfo"/> to their <see cref="ActiveCulture"/>.
/// Use <see cref="AllActiveCultures"/> to find <see cref="ActiveCulture"/> by their <see cref="ActiveCulture.Index"/>.
/// </para>
/// </summary>
public sealed class ActiveCultureSet : IReadOnlyDictionary<NormalizedCultureInfo,ActiveCulture>
{
    readonly Dictionary<NormalizedCultureInfo, ActiveCulture> _index;
    readonly ImmutableArray<ActiveCulture> _all;

    /// <summary>
    /// Initializes a new <see cref="ActiveCultureSet"/>.
    /// It will alwats contain its <see cref="Root"/> even if <paramref name="cultures"/>
    /// is empty.
    /// </summary>
    /// <param name="cultures">The selection of cultures.</param>
    public ActiveCultureSet( IEnumerable<NormalizedCultureInfo> cultures )
    {
        var index = new Dictionary<NormalizedCultureInfo, ActiveCulture>();
        var all = new List<ActiveCulture>();
        var root = new ActiveCulture( this, NormalizedCultureInfo.CodeDefault, 0, null, ImmutableArray<ActiveCulture>.Empty );
        var rootPath = ImmutableCollectionsMarshal.AsImmutableArray( [root] );
        index.Add( NormalizedCultureInfo.CodeDefault, root );
        all.Add( root );
        foreach( var c in cultures )
        {
            if( !index.ContainsKey( c ) )
            {
                Register( index, all, rootPath, c );
            }
        }
        List<ActiveCulture>?[] children = new List<ActiveCulture>[index.Count];
        foreach( var c in all )
        {
            var p = c.Parent;
            if( p != null )
            {
                ref var child = ref children[p.Index];
                child ??= new List<ActiveCulture>();
                child.Add( c );
            }
        }
        for( int i = 0; i < children.Length; ++i )
        {
            var collected = children[i];
            all[i]._children = collected != null ? collected.ToImmutableArray() : ImmutableArray<ActiveCulture>.Empty;
        }
        _index = index;
        _all = all.ToImmutableArray();
    }

    public ActiveCulture this[NormalizedCultureInfo key] => _index[key];


    /// <summary>
    /// Gets all the active cultures indexed by their <see cref="ActiveCulture.Index"/>.
    /// </summary>
    public ImmutableArray<ActiveCulture> AllActiveCultures => _all;

    /// <summary>
    /// Gets the root <see cref="NormalizedCultureInfo.CodeDefault"/>.
    /// </summary>
    public ActiveCulture Root => _all[0];

    public IEnumerable<NormalizedCultureInfo> Keys => _index.Keys;

    public IEnumerable<ActiveCulture> Values => _index.Values;

    public int Count => ((IReadOnlyCollection<KeyValuePair<NormalizedCultureInfo, ActiveCulture>>)_index).Count;

    public bool ContainsKey( NormalizedCultureInfo key )
    {
        return _index.ContainsKey( key );
    }

    public IEnumerator<KeyValuePair<NormalizedCultureInfo, ActiveCulture>> GetEnumerator() => _index.GetEnumerator();

    public bool TryGetValue( NormalizedCultureInfo key, [MaybeNullWhen( false )] out ActiveCulture value ) => _index.TryGetValue( key, out value );

    IEnumerator IEnumerable.GetEnumerator() => _index.GetEnumerator();

    ActiveCulture Register( Dictionary<NormalizedCultureInfo, ActiveCulture> index,
                            List<ActiveCulture> all,
                            ImmutableArray<ActiveCulture> root,
                            NormalizedCultureInfo c )
    {
        ActiveCulture? newOne;
        if( c.Fallbacks.Length == 0 )
        {
            newOne = new ActiveCulture( this, c, index.Count, root[0], root );
        }
        else
        {
            NormalizedCultureInfo pKey = c.Fallbacks[0];
            if( !index.TryGetValue( pKey, out var parent ) )
            {
                parent = Register( index, all, root, pKey );
            }
            newOne = new ActiveCulture( this, c, index.Count, parent, parent.Path.Add( parent ) );
        }
        index.Add( c, newOne );
        all.Add( newOne );
        return newOne;
    }
}
