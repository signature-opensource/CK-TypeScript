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
    public IReadOnlyDictionary<string, FinalTranslationValue> Translations => _translations;

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
        var translations = MergeTranslations( other, out bool isAmbiguous );

        IReadOnlyCollection<FinalTranslationSet> children;
        if( _children.Count > 0 )
        {
            if( other._children.Count > 0 )
            {
                bool isAllMine = true;
                bool isAllTheir = true;
                var candidates = new List<FinalTranslationSet>();
                foreach( var mine in _children )
                {
                    var their = other._children.FirstOrDefault( o => o._culture == mine._culture );
                    var merged = their != null ? mine.Aggregate( their ) : mine;
                    isAmbiguous |= merged._isAmbiguous;
                    candidates.Add( merged );
                    isAllMine &= merged == mine;
                    isAllTheir &= merged == their;
                }
                foreach( var their in other.Children )
                {
                    var alreadyMine = Children.FirstOrDefault( o => o._culture == their._culture );
                    if( alreadyMine == null )
                    {
                        isAmbiguous |= their._isAmbiguous;
                        candidates.Add( their );
                        isAllMine = false;
                    }
                }
                children = isAllMine
                            ? _children
                            : isAllTheir
                               ? other._children
                               : candidates;
            }
            else
            {
                children = _children;
                isAmbiguous |= _isAmbiguous;
            }
        }
        else
        {
            children = other._children;
            if( children != null )
            {
                isAmbiguous |= other._isAmbiguous;
            }
        }
        if( translations == _translations && children == _children )
        {
            Throw.DebugAssert( isAmbiguous == _isAmbiguous );
            return this;
        }
        if( translations == other._translations && children == other._children )
        {
            Throw.DebugAssert( isAmbiguous == other._isAmbiguous );
            return other;
        }
        return new FinalTranslationSet( translations, _culture, children, isAmbiguous );
    }

    IReadOnlyDictionary<string, FinalTranslationValue> MergeTranslations( FinalTranslationSet other, out bool isAmbiguous )
    {
        IReadOnlyDictionary<string, FinalTranslationValue> translations;
        if( _translations.Count > 0 )
        {
            if( other._translations.Count > 0 )
            {
                // Iterate on the smallest to add/update into the biggest.
                var (s1, s2) = (this, other);
                if( _translations.Count > other._translations.Count ) (s1, s2) = (s2, s1);
                var result = new Dictionary<string, FinalTranslationValue>( s2._translations );
                bool changed = false;
                isAmbiguous = s2._isAmbiguous;
                foreach( var (key, t1) in s1._translations )
                {
                    if( result.TryGetValue( key, out var t2 ) )
                    {
                        var f = t1.AddAmbiguities( t2.Ambiguities );
                        if( f.Ambiguities != t1.Ambiguities )
                        {
                            isAmbiguous |= f.Ambiguities != null;
                            result[key] = f;
                            changed = true;
                        }
                    }
                    else
                    {
                        result.Add( key, t1 );
                        changed = true;
                    }
                }
                translations = changed ? result : s2._translations;
            }
            else
            {
                translations = _translations;
                isAmbiguous = _isAmbiguous;
            }
        }
        else
        {
            translations = other._translations;
            isAmbiguous = other._isAmbiguous;
        }
        return translations;
    }
}
