using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.EmbeddedResources;

/// <summary>
/// Captures a "locales/" folder: the root culture is "en" and is filled with the
/// "default.json" or "default.jsonc" required file.
/// <para>
/// The hierarchy is under control of the <see cref="NormalizedCultureInfo"/> trees, itself
/// under control of the <see cref="System.Globalization.CultureInfo.Parent"/> structure.
/// </para>
/// </summary>
public sealed partial class TranslationDefinitionSet
{
    readonly NormalizedCultureInfo _culture;
    readonly ResourceLocator _origin;
    readonly IReadOnlyDictionary<string, TranslationDefinition> _translations;
    readonly TranslationDefinitionSet? _parent;
    TranslationDefinitionSet? _firstChild;
    TranslationDefinitionSet? _nextChild;

    List<TranslationDefinitionSet>? _children;

    internal TranslationDefinitionSet( ResourceLocator origin,
                                       NormalizedCultureInfo c,
                                       TranslationDefinitionSet? parent,
                                       IReadOnlyDictionary<string, TranslationDefinition>? translations )
    {
        Throw.DebugAssert( (parent == null) == c.IsDefault );
        _parent = parent;
        if( parent != null )
        {
            _nextChild = parent._firstChild;
            parent._firstChild = this;
        }
        _origin = origin;
        _culture = c;
        _translations = translations ?? ImmutableDictionary<string,TranslationDefinition>.Empty;
    }

    /// <summary>
    /// Creates a new definition set with its initial translations and children.
    /// This is typically used to restore a serialized state.
    /// </summary>
    /// <param name="origin">The <see cref="Origin"/>.</param>
    /// <param name="c">The <see cref="Culture"/>.</param>
    /// <param name="translations">The <see cref="Translations"/> if any.</param>
    /// <param name="children">The <see cref="Children"/> if any.</param>
    public static TranslationDefinitionSet UnsafeCreate( ResourceLocator origin,
                                                         NormalizedCultureInfo c,
                                                         Dictionary<string, TranslationDefinition>? translations,
                                                         List<TranslationDefinitionSet>? children )
    {
        var set = new TranslationDefinitionSet( origin, c, translations );
        set._children = children;
        return set;
    }

    /// <summary>
    /// Gets the translations.
    /// </summary>
    public IReadOnlyDictionary<string, TranslationDefinition> Translations => _translations;

    /// <summary>
    /// Gets the culture of this set.
    /// </summary>
    public NormalizedCultureInfo Culture => _culture;

    /// <summary>
    /// Gets the origin of this set.
    /// </summary>
    public ResourceLocator Origin => _origin;

    /// <summary>
    /// Gets the children (more specific culture sets).
    /// </summary>
    public IReadOnlyCollection<TranslationDefinitionSet> Children => (IReadOnlyCollection<TranslationDefinitionSet>?)_children ?? Array.Empty<TranslationDefinitionSet>();

    /// <summary>
    /// Gets all the translation sets, starting with this one (depth-first traversal).
    /// </summary>
    public IEnumerable<TranslationDefinitionSet> FlattenedAll
    {
        get
        {
            yield return this;
            if( _children != null )
            {
                foreach( var child in _children )
                {
                    foreach( var c in child.FlattenedAll )
                    {
                        yield return c;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Creates a new initial <see cref="FinalResourceAssetSet"/> from this one that is independent
    /// of any other asset definitions.
    /// <para>
    /// No <see cref="TranslationDefinition.Override"/> in <see cref="Translations"/> cannot be <see cref="ResourceOverrideKind.Regular"/>
    /// otherwise it is an error and null is returned.
    /// </para>
    /// <para>
    /// <see cref="ResourceOverrideKind.Optional"/> are silently ignored: only <see cref="ResourceOverrideKind.None"/>
    /// and <see cref="ResourceOverrideKind.Always"/> are kept in the resulting set.
    /// </para>
    /// </summary>
    public FinalTranslationSet? ToInitialFinalSet( IActivityMonitor monitor )
    {
        List<FinalTranslationSet>? children = null;
        if( _children != null )
        {
            children = new List<FinalTranslationSet>( _children.Count );
            for( int i = 0; i < _children.Count; i++ )
            {
                var c = _children[i].ToInitialFinalSet( monitor );
                if( c == null ) return null;
                children[i] = c;
            }
        }
        // Cannot be ambiguous by design.
        return new FinalTranslationSet( CreateFinalTranslations( monitor, _translations, _origin ), _culture, children, isAmbiguous: false );

        static Dictionary<string, FinalTranslationValue>? CreateFinalTranslations( IActivityMonitor monitor,
                                                                                   IReadOnlyDictionary<string,TranslationDefinition> definitions,
                                                                                   ResourceLocator origin )
        {
            var buggyOverrides = definitions.Where( kv => kv.Value.Override is ResourceOverrideKind.Regular );
            if( buggyOverrides.Any() )
            {
                monitor.Error( $"""
                    Invalid final set of resources {origin}. No asset can be defined as an override, the following resources are override definitions:
                    {buggyOverrides.Select( kv => kv.Key ).Concatenate()}
                    """ );
                return null;
            }
            var result = new Dictionary<string, FinalTranslationValue>( definitions.Count );
            foreach( var (key, definition) in definitions )
            {
                // Regular has trigerred the error above. Here we ignore Optional. 
                if( definition.Override is ResourceOverrideKind.None or ResourceOverrideKind.Always )
                {
                    result.Add( key, new FinalTranslationValue( definition.Text, origin ) );
                }
            }
            return result;
        }

    }


    /// <summary>
    /// Apply this set of definitions to a base <see cref="FinalTranslationSet"/> to produce a new
    /// final set. This can create ambiguities as well as removing ones.
    /// <para>
    /// This operation is not idempotent. When applied twice on a set, false ambiguities
    /// will be created.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="baseSet">The base set to consider.</param>
    /// <returns>A final set or null on error.</returns>
    public FinalTranslationSet? Combine( IActivityMonitor monitor, FinalTranslationSet baseSet )
    {
        List<FinalTranslationSet>? children = null;
        if( _children != null )
        {
            children = new List<FinalTranslationSet>( _children.Count );
            foreach( var cDef in _children )
            {

                var c = _children[i].ToInitialFinalSet( monitor );
                if( c == null ) return null;
                children[i] = c;
            }
        }

        static IReadOnlyDictionary<string, FinalTranslationValue>? CombineTranslations( IActivityMonitor monitor,
                                                                                        ResourceLocator defOrigin,
                                                                                        IReadOnlyDictionary<string, TranslationDefinition> definitions,
                                                                                        IReadOnlyDictionary<string, FinalTranslationValue> baseSet )
        {
            if( definitions.Count == 0 ) return baseSet;
            bool success = true;
            var newOne = new Dictionary<string, FinalTranslationValue>( baseSet );
            foreach( var (key,def) in definitions )
            {
                if( newOne.TryGetValue( key, out var exists ) )
                {
                    if( def.Override is ResourceOverrideKind.None )
                    {
                        monitor.Error( $"""
                                        Key '{key}' in {defOrigin} overides an existing value:
                                        This key is already defined by '{exists.Origin}' with the value:
                                        {exists.Text}
                                        The new value would be:
                                        {def.Text}
                                        Use Override prefix ('O:{key}') to allow this.
                                        """ );
                        success = false;
                    }
                    else
                    {
                        // Whether it is a "O", "?O" or "!O" we don't care here as we override.
                        if( def.Text == exists.Text )
                        {
                            monitor.Warn( $"""
                                            Useless override 'O:{key}' in {defOrigin}: it has the same value as the one from {exists.Origin}:
                                            {def.Text}
                                            """ );
                        }
                        else
                        {
                            // Override
                            newOne[key] = new FinalTranslationValue( def.Text, defOrigin );
                        }
                    }
                }
                else
                {
                    if( def.Override is ResourceOverrideKind.Regular )
                    {
                        monitor.Warn( $"Invalid override 'O:{key}' in {defOrigin}: the key doesn't exist, there's nothing to override." );
                    }
                    else
                    {
                        if( def.Override != ResourceOverrideKind.Optional )
                        {
                            newOne.Add( key, new FinalTranslationValue( def.Text, defOrigin ) );
                        }
                    }
                }
            }
            return success ? newOne : null;
        }

    }

    internal void AddSpecific( TranslationDefinitionSet specificSet )
    {
        _children ??= new List<TranslationDefinitionSet>();
        _children.Add( specificSet );
    }

    internal TranslationDefinitionSet? Find( NormalizedCultureInfo c )
    {
        if( c == _culture ) return this;
        if( _children != null )
        {
            foreach( var child in _children )
            {
                var r = child.Find( c );
                if( r != null ) return r;
            }
        }
        return null;
    }

    internal TranslationDefinitionSet? FindClosest( NormalizedCultureInfo c )
    {
        if( c == _culture ) return this;
        foreach( var f in c.Fallbacks )
        {
            var r = Find( f );
            if( r != null ) return r;
        }
        return null;
    }

    internal bool Remove( TranslationDefinitionSet s )
    {
        if( _children != null )
        {
            if( _children.Remove( s ) )
            {
                if( s._children != null )
                {
                    _children.AddRange( s._children );
                }
                return true;
            }
            foreach( var child in _children )
            {
                if( child.Remove( s ) ) return true;
            }
        }
        return false;
    }
}
