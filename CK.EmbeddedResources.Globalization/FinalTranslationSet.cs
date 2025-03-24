using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.EmbeddedResources;

public sealed class FinalTranslationSet
{
    readonly IReadOnlyDictionary<string, FinalTranslationValue> _translations;
    readonly NormalizedCultureInfo _culture;
    readonly IReadOnlyCollection<FinalTranslationSet> _children;
    readonly bool _isAmbiguous;

    internal FinalTranslationSet( IReadOnlyDictionary<string, FinalTranslationValue>? translations,
                                  NormalizedCultureInfo culture,
                                  IReadOnlyCollection<FinalTranslationSet>? children,
                                  bool isAmbiguous )
    {
        _translations = translations ?? ImmutableDictionary<string,FinalTranslationValue>.Empty;
        _culture = culture;
        _children = children ?? Array.Empty<FinalTranslationSet>();
        _isAmbiguous = isAmbiguous;
    }

    /// <summary>
    /// Gets the translations.
    /// </summary>
    public IReadOnlyDictionary<string, FinalTranslationValue>? Translations => _translations;

    /// <summary>
    /// Gets the culture of this set.
    /// </summary>
    public NormalizedCultureInfo Culture => _culture;

    /// <summary>
    /// Gets whether at least one of the <see cref="FinalTranslationValue"/> from these translations
    /// or from a child has a non null <see cref="FinalTranslationValue.Ambiguities"/>.
    /// </summary>
    public bool IsAmbiguous => _isAmbiguous || (_children?.Any( c => c.IsAmbiguous ) ?? false);

    /// <summary>
    /// Gets the children (more specific culture sets).
    /// </summary>
    public IReadOnlyCollection<FinalTranslationSet> Children => _children;

    /// <summary>
    /// Aggregate this set with an other one. Even if both sets are not <see cref="IsAmbiguous"/>,
    /// if a common key is mapped to different text, the result will be ambiguous.
    /// </summary>
    /// <param name="other">The other set to aggregate.</param>
    /// <returns>The aggregated set.</returns>
    public FinalTranslationSet Aggregate( FinalTranslationSet other )
    {
        // Iterate on the smallest to add/update into the biggest.
        var (s1, s2) = (this, other);
        if( _translations.Count > other._translations.Count ) (s1, s2) = (s2, s1);
        var result = new Dictionary<string, FinalTranslationValue>( s2._translations );
        bool isAmbiguous = s2._isAmbiguous;
        foreach( var (key, t1) in s1._translations )
        {
            if( result.TryGetValue( key, out var t2 ) )
            {
                var f = t1.AddAmbiguities( t2.Ambiguities );
                isAmbiguous |= f.Ambiguities != null;
                result[key] = f;
            }
            else
            {
                result.Add( key, t1 );
            }
        }
        IReadOnlyCollection<FinalTranslationSet> children;
        if( _children.Count > 0 )
        {
            if( other._children.Count > 0 )
            {
                var newOnes = new List<FinalTranslationSet>();
                foreach( var mine in _children )
                {
                    var their = other._children.FirstOrDefault( c => c.Culture == mine._culture );
                    if( their != null )
                    {
                        their = mine.Aggregate( their );
                        isAmbiguous |= their.IsAmbiguous;
                        newOnes.Add( their );
                    }
                    else
                    {
                        newOnes.Add( mine );
                    }
                }
                children = newOnes;
            }
            else
            {
                children = _children;
            }
        }
        else
        {
            children = other._children;
        }
        return new FinalTranslationSet( result, _culture, children, isAmbiguous );
    }

}
