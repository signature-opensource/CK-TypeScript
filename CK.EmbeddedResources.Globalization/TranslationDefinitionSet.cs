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
    readonly IReadOnlyDictionary<string, TranslationDefinition> _definitions;
    readonly ITranslationDefinitionSet?[] _subSets;

    internal TranslationDefinitionSet( ActiveCultureSet activeCultures,
                                       ResourceLocator origin,
                                       IReadOnlyDictionary<string, TranslationDefinition>? translations )
    {
        _activeCultures = activeCultures;
        _origin = origin;
        _definitions = translations ?? ImmutableDictionary<string, TranslationDefinition>.Empty;
        _subSets = new ITranslationDefinitionSet[activeCultures.Count];
        _subSets[0] = this;
    }

    /// <summary>
    /// Gets the cultures set to which this definition is bound.
    /// </summary>
    public ActiveCultureSet ActiveCultures => _activeCultures;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, TranslationDefinition> Translations => _definitions;

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
        var translations = CreateInitialTranslations( monitor, this );
        bool success = translations != null;
        var subSets = new IFinalTranslationSet[_subSets.Length];
        var result = new FinalTranslationSet( _activeCultures, translations, subSets, isAmbiguous: false );
        for( int i = 1; i < subSets.Length; ++i )
        {
            var def = _subSets[i];
            if( def != null )
            {
                translations = CreateInitialTranslations( monitor, def );
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

    }

    static Dictionary<string, FinalTranslationValue>? CreateInitialTranslations( IActivityMonitor monitor,
                                                                                 ITranslationDefinitionSet definition )
    {
        var buggyOverrides = definition.Translations.Where( kv => kv.Value.Override is ResourceOverrideKind.Regular );
        if( buggyOverrides.Any() )
        {
            monitor.Error( $"""
                    Invalid initial set of translation definitions {definition.Origin}.
                    No translation can be defined as regular override ("O:"). Only optional ("O?" - that will be skipped) and always ("O!:" - that will be kept) are allowed.
                    The following resources are regular override definitions:
                    {buggyOverrides.Select( kv => kv.Key ).Concatenate()}
                    """ );
            return null;
        }
        var result = new Dictionary<string, FinalTranslationValue>( definition.Translations.Count );
        foreach( var (key, def) in definition.Translations )
        {
            // Regular has trigerred the error above. Here we ignore Optional. 
            if( def.Override is ResourceOverrideKind.None or ResourceOverrideKind.Always )
            {
                result.Add( key, new FinalTranslationValue( def.Text, definition.Origin ) );
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
        var (rootTranslations, isAmbiguous) = CombineTranslations( monitor, this, baseSet, isRoot: true );
        bool success = rootTranslations != null;

        var subSets = baseSet.CloneSubSets();
        // We create a result only when actually needed.
        FinalTranslationSet? result = null;
        for( int i = 1; i < subSets.Length; ++i )
        {
            var otherSet = subSets[i];
            var def = _subSets[i];
            if( def == null )
            {
                if( otherSet != null )
                {
                    isAmbiguous |= otherSet.IsAmbiguous;
                }
            }
            else
            {
                if( otherSet == null )
                {
                    var t = CreateInitialTranslations( monitor, def );
                    success &= t != null;
                    if( success )
                    {
                        Throw.DebugAssert( t != null );
                        result ??= new FinalTranslationSet( _activeCultures, rootTranslations, subSets, false );
                        subSets[i] = new FinalTranslationSet.SubSet( result, def.Culture, t, isAmbiguous: false );
                    }
                }
                else
                {
                    var (t, a) = CombineTranslations( monitor, def, otherSet, isRoot: false );
                    success &= t != null;
                    if( success )
                    {
                        Throw.DebugAssert( t != null );
                        result ??= new FinalTranslationSet( _activeCultures, rootTranslations, subSets, false );
                        subSets[i] = new FinalTranslationSet.SubSet( result, def.Culture, t, a );
                        isAmbiguous |= a;
                    }
                }
            }
        }
        if( success )
        {
            if( result == null )
            {
                if( rootTranslations == baseSet.Translations )
                {
                    return baseSet;
                }
                result = new FinalTranslationSet( _activeCultures, rootTranslations, subSets, false );
            }
            result._isAmbiguous = isAmbiguous;
            return result;
        }
        return null;
    }

    static (IReadOnlyDictionary<string, FinalTranslationValue>?,bool) CombineTranslations( IActivityMonitor monitor,
                                                                                           ITranslationDefinitionSet definition,
                                                                                           IFinalTranslationSet baseSet,
                                                                                           bool isRoot )
    {
        bool success = true;
        bool isAmbiguous = !isRoot && baseSet.IsAmbiguous;
        bool mustRecomputeAmbiguities = isRoot;
        var result = baseSet.Translations;
        if( definition.Translations.Count > 0 )
        {
            bool changed = false;
            var newOne = new Dictionary<string, FinalTranslationValue>( baseSet.Translations );
            foreach( var (key, def) in definition.Translations )
            {
                bool fromInheritance = false;
                if( !newOne.TryGetValue( key, out var exists ) )
                {
                    var p = baseSet.Parent;
                    while( p != null && !p.Translations.TryGetValue( key, out exists ) )
                    {
                        p = p.Parent;
                    }
                    fromInheritance = true;
                }
                if( exists.IsValid )
                {
                    if( def.Override is ResourceOverrideKind.None )
                    {
                        monitor.Error( $"""
                                    Key '{key}' in {definition.Origin} overides an existing value:
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
                        bool hasAmbiguities = exists.Ambiguities != null;
                        if( !hasAmbiguities && def.Text == exists.Text )
                        {
                            monitor.Warn( $"""
                                       Useless override 'O:{key}' in {definition.Origin}: it has the same value as the one from {exists.Origin}:
                                       {def.Text}
                                       """ );
                        }
                        else
                        {
                            // Override (clears any ambiguity).
                            newOne[key] = new FinalTranslationValue( def.Text, definition.Origin );
                            mustRecomputeAmbiguities = hasAmbiguities && !fromInheritance;
                            changed = true;
                        }
                    }
                }
                else
                {
                    if( def.Override is ResourceOverrideKind.Regular )
                    {
                        monitor.Warn( $"Invalid override 'O:{key}' in {definition.Origin}: the key doesn't exist, there's nothing to override." );
                    }
                    else
                    {
                        if( def.Override != ResourceOverrideKind.Optional )
                        {
                            newOne.Add( key, new FinalTranslationValue( def.Text, definition.Origin ) );
                            changed = true;
                        }
                    }
                }
            }
            if( changed ) result = newOne;
        }
        if( success )
        {
            if( mustRecomputeAmbiguities )
            {
                isAmbiguous = result.Values.Any( v => v.Ambiguities != null );
            }
            return (result,isAmbiguous);
        }
        return default;
    }



}
