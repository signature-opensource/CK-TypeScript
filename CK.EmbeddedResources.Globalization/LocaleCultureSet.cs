using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Captures a "locales/" folder: the root culture is "en" and is filled with the
/// "default.json" or "default.jsonc" required file.
/// <para>
/// The hierarchy is under control of the <see cref="NormalizedCultureInfo"/> trees, itself
/// under control of the <see cref="System.Globalization.CultureInfo.Parent"/> structure.
/// </para>
/// <para>
/// Even if this can be created manually, <see cref="ResourceContainerGlobalizationExtension.LoadLocales"/> do the hard job.
/// </para>
/// </summary>
public sealed partial class LocaleCultureSet
{
    readonly NormalizedCultureInfo _culture;
    readonly Core.ResourceLocator _origin;
    Dictionary<string, TranslationValue>? _translations;
    List<LocaleCultureSet>? _children;

    /// <summary>
    /// Copy constructor. This does a deep copy of the <paramref name="set"/>.
    /// </summary>
    /// <param name="set">The set to copy.</param>
    public LocaleCultureSet( LocaleCultureSet set )
    {
        _culture = set._culture;
        _origin = set._origin;
        if( set._translations != null )
        {
            _translations = new Dictionary<string, TranslationValue>( set._translations );
        }
        if( set._children != null )
        {
            _children = new List<LocaleCultureSet>( set._children.Select( s => new LocaleCultureSet( s ) ) );
        }
    }

    public LocaleCultureSet( ResourceLocator origin, NormalizedCultureInfo c )
        : this( origin, c, null )
    {
    }

    internal LocaleCultureSet( ResourceLocator origin, NormalizedCultureInfo c, Dictionary<string, TranslationValue>? translations )
    {
        _origin = origin;
        _culture = c;
        _translations = translations;
    }

    /// <summary>
    /// Creates a new culture set with its initial translations and children.
    /// This is typically used to restore a serialized state.
    /// </summary>
    /// <param name="origin">The <see cref="Origin"/>.</param>
    /// <param name="c">The <see cref="Culture"/>.</param>
    /// <param name="translations">The <see cref="Translations"/> if any.</param>
    /// <param name="children">The <see cref="Children"/> if any.</param>
    public static LocaleCultureSet UnsafeCreate( ResourceLocator origin,
                                                 NormalizedCultureInfo c,
                                                 Dictionary<string, TranslationValue>? translations,
                                                 List<LocaleCultureSet>? children )
    {
        var set = new LocaleCultureSet( origin, c, translations );
        set._children = children;
        return set;
    }

    /// <summary>
    /// Gets whether at least one translation exists (without allocating an empty <see cref="Translations"/>).
    /// </summary>
    public bool HasTranslations => _translations != null && _translations.Count > 0;

    /// <summary>
    /// Gets the mutable translations.
    /// </summary>
    public Dictionary<string, TranslationValue> Translations => _translations ??= new Dictionary<string, TranslationValue>();

    /// <summary>
    /// Gets the culture of this set.
    /// </summary>
    public NormalizedCultureInfo Culture => _culture;

    /// <summary>
    /// Gets the origin of this set.
    /// </summary>
    public Core.ResourceLocator Origin => _origin;

    /// <summary>
    /// Gets the children (more specific culture sets).
    /// </summary>
    public IReadOnlyCollection<LocaleCultureSet> Children => (IReadOnlyCollection<LocaleCultureSet>?)_children ?? Array.Empty<LocaleCultureSet>();

    /// <summary>
    /// Gets all the culture sets, starting with this one (depth-first traversal).
    /// </summary>
    public IEnumerable<LocaleCultureSet> FlattenedAll
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

    internal void AddSpecific( LocaleCultureSet specificSet )
    {
        _children ??= new List<LocaleCultureSet>();
        _children.Add( specificSet );
    }

    internal LocaleCultureSet? Find( NormalizedCultureInfo c )
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

    internal LocaleCultureSet? FindClosest( NormalizedCultureInfo c )
    {
        if( c == _culture ) return this;
        foreach( var f in c.Fallbacks )
        {
            var r = Find( f );
            if( r != null ) return r;
        }
        return null;
    }

    internal bool Remove( LocaleCultureSet s )
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
