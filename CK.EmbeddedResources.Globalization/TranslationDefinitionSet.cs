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
    List<TranslationDefinitionSet>? _children;

    internal TranslationDefinitionSet( ResourceLocator origin,
                                       NormalizedCultureInfo c,
                                       IReadOnlyDictionary<string, TranslationDefinition>? translations )
    {
        _origin = origin;
        _culture = c;
        _translations = translations ?? ImmutableDictionary<string,TranslationDefinition>.Empty;
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
    /// Creates a new initial <see cref="FinalTranslationSet"/> from this definition that is independent
    /// of any other translation definition.
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
        var translations = CreateFinalTranslations( monitor );
        bool success = translations != null;

        IReadOnlyCollection<FinalTranslationSet>? children = null;
        if( _children != null )
        {
            // Not easy here :-(.
            // We must enure a compact tree when the definitions are not compact.
            // Only definitions for NeutralCultures are assumed while reading the resources.
            // Children may not have a parent definitions if the parent is not a Neutral culture.
            //
            // This seems overly complicated but I failed to find a more elegant solution without
            // dangerous (and hideous) mutabilites...
            //
            // And the bad news is that the Combine has the same compacity issue.
            //
            var buildMap = _children.SelectMany( def => def._culture.Fallbacks.Append( def._culture ) )
                                    .Distinct()
                                    .OrderByDescending( c => c.Fallbacks.Length )
                                    .Select( (c,index) => (Index: index, Culture: c, Definitions: _children.FirstOrDefault( d => d._culture == c )) )
                                    .ToArray();
            var buffer = new FinalTranslationSet?[buildMap.Length];
            List<FinalTranslationSet>? childrenBuilder = null;
            foreach( var b in buildMap )
            {
                // Collects the children of this culture that have already been
                // built (thanks to the OrderByDescending).
                for( int i = 0; i < b.Index; ++i )
                {
                    var candidate = buffer[i];
                    if( candidate != null
                        && candidate.Culture.Fallbacks.Length > 0
                        && candidate.Culture.Fallbacks[0] == b.Culture )
                    {
                        // We have a child of this culture. Acquire it.
                        childrenBuilder ??= new List<FinalTranslationSet>();
                        childrenBuilder.Add( candidate );
                        buffer[i] = null;
                    }
                }
                FinalTranslationSet[]? childChildren = null;
                if( childrenBuilder != null && childrenBuilder.Count > 0 )
                {
                    childChildren = childrenBuilder.ToArray();
                    childrenBuilder.Clear();
                }
                Throw.DebugAssert( "If we have no definitions, then we have children.", b.Definitions != null || childChildren != null );
                IReadOnlyDictionary<string, FinalTranslationValue>? childTranslations = null;
                if( b.Definitions != null )
                {
                    childTranslations = CreateFinalTranslations( monitor );
                    success &= childChildren != null;
                }
                // Cannot be ambiguous by design.
                buffer[b.Index] = new FinalTranslationSet( childTranslations, b.Culture, childChildren, isAmbiguous: false );
            }
            // The non null cultures that remains in the buffer are our children.
            if( success )
            {
                children = buffer.Where( f => f != null ).ToArray()!;
            }
        }
        // Cannot be ambiguous by design.
        return new FinalTranslationSet( translations, _culture, children, isAmbiguous: false );

    }

    Dictionary<string, FinalTranslationValue>? CreateFinalTranslations( IActivityMonitor monitor )
    {
        var buggyOverrides = _translations.Where( kv => kv.Value.Override is ResourceOverrideKind.Regular );
        if( buggyOverrides.Any() )
        {
            monitor.Error( $"""
                    Invalid final set of resources {_origin}. No asset can be defined as an override, the following resources are override definitions:
                    {buggyOverrides.Select( kv => kv.Key ).Concatenate()}
                    """ );
            return null;
        }
        var result = new Dictionary<string, FinalTranslationValue>( _translations.Count );
        foreach( var (key, definition) in _translations )
        {
            // Regular has trigerred the error above. Here we ignore Optional. 
            if( definition.Override is ResourceOverrideKind.None or ResourceOverrideKind.Always )
            {
                result.Add( key, new FinalTranslationValue( definition.Text, _origin ) );
            }
        }
        return result;
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
        Throw.CheckArgument( baseSet.Culture == Culture );
        var translations = CombineTranslations( monitor, _origin, _translations, null, baseSet.Translations );
        bool success = translations != null;

        List<FinalTranslationSet>? children = null;
        if( _children != null )
        {
            children = new List<FinalTranslationSet>( _children.Count );
            foreach( var cDef in _children )
            {
                // BuildChildPath
                var inheritFinder = BuildChildPath( baseSet, cDef.Culture, out var exactSet );
                var c = _children[i].ToInitialFinalSet( monitor );
                if( c == null ) return null;
                children[i] = c;
            }
        }

        static Func<string, FinalTranslationValue> BuildChildPath( FinalTranslationSet baseSet, NormalizedCultureInfo c, out FinalTranslationSet? cSet )
        {
            cSet = null;
            Func<string, FinalTranslationValue>? finder = null;
            foreach( var fC in baseSet.Children )
            {
                if( fC.Culture == c )
                {
                    cSet = fC;
                    break;
                }
                foreach( var pC in c.Fallbacks )
                {
                    if( fC.Culture == pC )
                    {
                        finder = BuildChildPath( fC, c, out cSet );
                    }
                }
            }
            if( finder == null )
            {
                return key => baseSet.Translations.GetValueOrDefault( key );
            }
            return key =>
            {
                var e = finder( key );
                return e.IsValid ? e : baseSet.Translations.GetValueOrDefault( key );
            };
        }
    }

    static IReadOnlyDictionary<string, FinalTranslationValue>? CombineTranslations( IActivityMonitor monitor,
                                                                                    ResourceLocator defOrigin,
                                                                                    IReadOnlyDictionary<string, TranslationDefinition> definitions,
                                                                                    Func<string, FinalTranslationValue>? inheritFinder,
                                                                                    IReadOnlyDictionary<string, FinalTranslationValue> baseSet )
    {
        if( definitions.Count == 0 ) return baseSet;
        bool success = true;
        var newOne = new Dictionary<string, FinalTranslationValue>( baseSet );
        foreach( var (key, def) in definitions )
        {
            var existsInBaseSet = newOne.TryGetValue( key, out var exists );
            var existsInherited = existsInBaseSet || (inheritFinder != null && (exists = inheritFinder( key )).IsValid);
            if( existsInherited )
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
                    // We warn for a useless (same) text, except if this override resolves an ambiguity.
                    if( exists.Ambiguities != null && def.Text == exists.Text )
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
        Throw.DebugAssert( "Called on the default set only.", _culture == NormalizedCultureInfo.CodeDefault );
        Throw.DebugAssert( "Called only for specific cultures.", !c.IsNeutralCulture && !c.IsDefault );
        foreach( var f in c.Fallbacks )
        {
            var r = Find( f );
            if( r != null ) return r;
        }
        return null;
    }
}
