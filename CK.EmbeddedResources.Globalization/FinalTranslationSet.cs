using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.EmbeddedResources;

public sealed partial class FinalTranslationSet : IFinalTranslationSet
{
    readonly IReadOnlyDictionary<string, FinalTranslationValue> _translations;
    readonly ActiveCultureSet _activeCultures;
    readonly bool _isAmbiguous;
    readonly IFinalTranslationSet?[] _subSets;

    internal FinalTranslationSet( ActiveCultureSet activeCultures,
                                  IReadOnlyDictionary<string, FinalTranslationValue>? translations,
                                  IFinalTranslationSet?[] subSets,
                                  bool isAmbiguous )
    {
        _activeCultures = activeCultures;
        _translations = translations ?? ImmutableDictionary<string, FinalTranslationValue>.Empty;
        _subSets = subSets;
        _isAmbiguous = isAmbiguous;
        subSets[0] = this;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, FinalTranslationValue> Translations => _translations;

    /// <inheritdoc />
    public ActiveCulture Culture => _activeCultures.Root;

    /// <inheritdoc />
    public IEnumerable<IFinalTranslationSet> Children => Culture.Children.Select( c => _subSets[c.Index] ).Where( s => s != null )!;

    /// <inheritdoc />
    public bool IsAmbiguous => _isAmbiguous;

    /// <summary>
    /// Aggregate this set with an other one. Even if both sets are not <see cref="IsAmbiguous"/>,
    /// if a common key is mapped to different text, the result will be ambiguous.
    /// </summary>
    /// <param name="other">The other set to aggregate.</param>
    /// <returns>The aggregated set.</returns>
    public FinalTranslationSet Aggregate( FinalTranslationSet other )
    {
        Throw.CheckArgument( Culture == other.Culture );
        var translations = AggregateTranslations( this, other, out bool isAmbiguous );

        bool changed = false;
        var subSets = new IFinalTranslationSet[_subSets.Length];
        for( int i = 1; i < subSets.Length; ++i )
        {
            var mine = _subSets[i];
            var their = other._subSets[i];
            if( mine == null )
            {
                if( their != null )
                {
                    subSets[i] = their;
                    changed = true;
                    isAmbiguous |= their.IsAmbiguous;
                }
            }
            else
            {
                if( their == null )
                {
                    subSets[i] = mine;
                }
                else
                {
                    var agg = AggregateTranslations( mine, their, out var aggAmbiguous );
                    if( agg == mine )
                    {
                        subSets[i] = mine;
                    }
                    else
                    {
                        changed = true;
                        subSets[i] = agg == their
                                        ? their
                                        : new SubSet( this, mine.Culture, agg, aggAmbiguous );
                    }
                    isAmbiguous |= aggAmbiguous;
                }
            }
        }

        if( translations == _translations && !changed )
        {
            Throw.DebugAssert( isAmbiguous == _isAmbiguous );
            return this;
        }
        return new FinalTranslationSet( _activeCultures, translations, _subSets, isAmbiguous );
    }

    static IReadOnlyDictionary<string, FinalTranslationValue> AggregateTranslations( IFinalTranslationSet set1,
                                                                                     IFinalTranslationSet set2,
                                                                                     out bool isAmbiguous )
    {
        IReadOnlyDictionary<string, FinalTranslationValue> result;
        if( set1.Translations.Count > 0 )
        {
            if( set2.Translations.Count > 0 )
            {
                // Iterate on the smallest to add/update into the biggest.
                var (s1, s2) = (set1, set2);
                if( set1.Translations.Count > set2.Translations.Count ) (s1, s2) = (s2, s1);
                var candidate = new Dictionary<string, FinalTranslationValue>( s2.Translations );
                bool changed = false;
                isAmbiguous = s2.IsAmbiguous;
                foreach( var (key, t1) in s1.Translations )
                {
                    if( candidate.TryGetValue( key, out var t2 ) )
                    {
                        var f = t1.AddAmbiguities( t2.Ambiguities );
                        if( f.Ambiguities != t1.Ambiguities )
                        {
                            isAmbiguous |= f.Ambiguities != null;
                            candidate[key] = f;
                            changed = true;
                        }
                    }
                    else
                    {
                        candidate.Add( key, t1 );
                        changed = true;
                    }
                }
                result = changed ? candidate : s1.Translations;
            }
            else
            {
                result = set1.Translations;
                isAmbiguous = set1.IsAmbiguous;
            }
        }
        else
        {
            result = set2.Translations;
            isAmbiguous = set2.IsAmbiguous;
        }
        return result;
    }
}
