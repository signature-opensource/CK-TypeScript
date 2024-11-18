using CK.Core;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace CK.TypeScript.Engine;

/// <summary>
/// Captures the "ts-locales/" folder: the root culture is "en" and is filled with the
/// "default.json" or "default.jsonc" required file.
/// <para>
/// The hierarchy is under control of the <see cref="NormalizedCultureInfo"/> trees, itself
/// under control of the <see cref="System.Globalization.CultureInfo.Parent"/> structure.
/// </para>
/// <para>
/// Only cultures registered in <see cref="TypeScriptBinPathAspectConfiguration.ActiveCultures"/>
/// are handled, others are ignored.
/// </para>
/// </summary>
public sealed partial class TSLocaleCultureSet
{
    readonly NormalizedCultureInfo _culture;
    readonly Dictionary<string,string> _translations;
    readonly Core.ResourceLocator _origin;
    List<TSLocaleCultureSet>? _children;

    internal TSLocaleCultureSet( Core.ResourceLocator origin, NormalizedCultureInfo c, Dictionary<string, string> translations )
    {
        _origin = origin;
        _culture = c;
        _translations = translations;
    }

    /// <summary>
    /// Gets the translations.
    /// </summary>
    public Dictionary<string, string> Translations => _translations;

    /// <summary>
    /// Gets the culture of this set.
    /// </summary>
    public NormalizedCultureInfo Culture => _culture;

    /// <summary>
    /// Gets the origin of this set.
    /// </summary>
    public Core.ResourceLocator Origin => _origin;

    /// <summary>
    /// Gets the children, the more specific culture sets.
    /// </summary>
    public IEnumerable<TSLocaleCultureSet> Children => _children ?? Enumerable.Empty<TSLocaleCultureSet>();

    internal void AddSpecific( TSLocaleCultureSet specificSet )
    {
        _children ??= new List<TSLocaleCultureSet>();
        _children.Add( specificSet );
    }

    internal TSLocaleCultureSet? Find( NormalizedCultureInfo c )
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

    internal TSLocaleCultureSet? FindClosest( NormalizedCultureInfo c )
    {
        if( c == _culture ) return this;
        foreach( var f in c.Fallbacks )
        {
            var r = Find( f );
            if( r != null ) return r;
        }
        return null;
    }

}
