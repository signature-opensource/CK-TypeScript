using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;

namespace CK.EmbeddedResources;

/// <summary>
/// Captures a "locales/" folder: the root culture is "en" and is filled with the
/// "default.jsonc" required file.
/// <para>
/// The hierarchy is under control of the <see cref="ActiveCultures"/> tree, itself
/// under control of the <see cref="System.Globalization.CultureInfo.Parent"/> structure.
/// </para>
/// </summary>
public sealed partial class TranslationDefinitionSet : ITranslationDefinitionSet
{
    readonly ActiveCultureSet _activeCultures;
    readonly ResourceLocator _origin;
    readonly IReadOnlyDictionary<string, TranslationDefinition> _translations;
    readonly ITranslationDefinitionSet?[] _subSets;

    internal TranslationDefinitionSet( ActiveCultureSet activeCultures,
                                       ResourceLocator origin,
                                       IReadOnlyDictionary<string, TranslationDefinition>? translations )
    {
        _activeCultures = activeCultures;
        _origin = origin;
        _translations = translations ?? ImmutableDictionary<string, TranslationDefinition>.Empty;
        _subSets = new TranslationDefinitionSet[activeCultures.Count];
        _subSets[0] = this;
    }

    /// <summary>
    /// Gets the cultures set to which this definition is bound.
    /// </summary>
    public ActiveCultureSet ActiveCultures => _activeCultures;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, TranslationDefinition> Translations => _translations;

    /// <inheritdoc />
    public ActiveCulture Culture => _activeCultures.Root;

    /// <inheritdoc />
    public ResourceLocator Origin => _origin;

    /// <inheritdoc />
    public IEnumerable<ITranslationDefinitionSet> Children => Culture.Children.Select( c => _subSets[c.Index] ).Where( s => s != null )!;

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
        var translations = CreateFinalTranslations( monitor, _origin, _translations );
        bool success = translations != null;
        var subSets = new IFinalTranslationSet[_subSets.Length];
        var result = new FinalTranslationSet( _activeCultures, translations, subSets, isAmbiguous: false );
        for( int i = 1; i < subSets.Length; ++i )
        {
            var def = _subSets[i];
            if( def != null )
            {
                translations = CreateFinalTranslations( monitor, def.Origin, def.Translations );
                if( translations != null )
                {
                    subSets[i] = new FinalTranslationSet.SubSet( result, def.Culture, translations, isAmbiguous: false );
                }
                else
                {
                    success = false;
                }
            }
        }
        return success ? result : null;

        static Dictionary<string, FinalTranslationValue>? CreateFinalTranslations( IActivityMonitor monitor,
                                                                                   ResourceLocator origin,
                                                                                   IReadOnlyDictionary<string, TranslationDefinition> translations )
        {
            var buggyOverrides = translations.Where( kv => kv.Value.Override is ResourceOverrideKind.Regular );
            if( buggyOverrides.Any() )
            {
                monitor.Error( $"""
                        Invalid final set of resources {origin}. No asset can be defined as an override, the following resources are override definitions:
                        {buggyOverrides.Select( kv => kv.Key ).Concatenate()}
                        """ );
                return null;
            }
            var result = new Dictionary<string, FinalTranslationValue>( translations.Count );
            foreach( var (key, definition) in translations )
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
        Throw.CheckArgument( baseSet.Culture == Culture );
        var translations = CombineTranslations( monitor, _origin, _translations, null, baseSet.Translations );
        bool success = translations != null;

        var subSets = new IFinalTranslationSet[_subSets.Length];
        var result = new FinalTranslationSet( _activeCultures, translations, subSets, isAmbiguous: false );
        for( int i = 1; i < subSets.Length; ++i )
        {
            var def = _subSets[i];
            if( def != null )
            {
                translations = CreateFinalTranslations( monitor, def.Origin, def.Translations );
                if( translations != null )
                {
                    subSets[i] = new FinalTranslationSet.SubSet( result, def.Culture, translations, isAmbiguous: false );
                }
                else
                {
                    success = false;
                }
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



}
