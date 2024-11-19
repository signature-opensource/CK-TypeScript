//using CK.Core;
//using CK.Setup;
//using System.Collections.Generic;
//using System.Diagnostics.CodeAnalysis;
//using System.IO;
//using System.Linq;

//namespace CK.TypeScript.Engine;

///// <summary>
///// Captures the "ts-locales/" folder: the root culture is "en" and is filled with the
///// "default.json" or "default.jsonc" required file.
///// <para>
///// The hierarchy is under control of the <see cref="NormalizedCultureInfo"/> trees, itself
///// under control of the <see cref="System.Globalization.CultureInfo.Parent"/> structure.
///// </para>
///// <para>
///// Only cultures registered in <see cref="TypeScriptBinPathAspectConfiguration.ActiveCultures"/>
///// are handled, others are ignored.
///// </para>
///// </summary>
//public sealed partial class LocaleCultureSet
//{
//    readonly NormalizedCultureInfo _culture;
//    readonly Core.ResourceLocator _origin;
//    Dictionary<string, TranslationValue>? _translations;
//    List<LocaleCultureSet>? _children;

//    public LocaleCultureSet( Core.ResourceLocator origin, NormalizedCultureInfo c )
//        : this( origin, c, null )
//    {
//    }

//    internal LocaleCultureSet( Core.ResourceLocator origin, NormalizedCultureInfo c, Dictionary<string, TranslationValue>? translations )
//    {
//        _origin = origin;
//        _culture = c;
//        _translations = translations;
//    }

//    /// <summary>
//    /// Gets the translations.
//    /// </summary>
//    public Dictionary<string, TranslationValue> Translations => _translations ??= new Dictionary<string, TranslationValue>();

//    /// <summary>
//    /// Gets the culture of this set.
//    /// </summary>
//    public NormalizedCultureInfo Culture => _culture;

//    /// <summary>
//    /// Gets the origin of this set.
//    /// </summary>
//    public Core.ResourceLocator Origin => _origin;

//    /// <summary>
//    /// Gets the children, the more specific culture sets.
//    /// </summary>
//    public IEnumerable<LocaleCultureSet> Children => _children ?? Enumerable.Empty<LocaleCultureSet>();

//    internal void AddSpecific( LocaleCultureSet specificSet )
//    {
//        _children ??= new List<LocaleCultureSet>();
//        _children.Add( specificSet );
//    }

//    internal LocaleCultureSet? Find( NormalizedCultureInfo c )
//    {
//        if( c == _culture ) return this;
//        if( _children != null )
//        {
//            foreach( var child in _children )
//            {
//                var r = child.Find( c );
//                if( r != null ) return r;
//            }
//        }
//        return null;
//    }

//    internal LocaleCultureSet? FindClosest( NormalizedCultureInfo c )
//    {
//        if( c == _culture ) return this;
//        foreach( var f in c.Fallbacks )
//        {
//            var r = Find( f );
//            if( r != null ) return r;
//        }
//        return null;
//    }



//    // Quick & Dirty implementation: as this is done at the end of process, we lift the content
//    // instead of creating intermediate dictionaries and mutate them.
//    internal bool MergeWith( IActivityMonitor monitor, LocaleCultureSet above )
//    {
//        bool success = true;
//        if( _translations == null || _translations.Count == 0 )
//        {
//            _translations = above._translations;
//        }
//        else if( above._translations != null && above._translations.Count > 0 )
//        {
//            success &= MergeTranslations( monitor, _translations, above._translations );
//        }
//        if( above._children != null )
//        {
//            if( _children == null )
//            {
//                _children = above._children;
//            }
//            else
//            {
//                foreach( var a in above._children )
//                {
//                    var mine = Find( a._culture );
//                    if( mine != null )
//                    {
//                        success &= mine.MergeWith( monitor, a );
//                    }
//                    else
//                    {
//                        var mineParent = FindClosest( a._culture );
//                        if( mineParent != null )
//                        {
//                            mineParent.AddSpecific( a );
//                        }
//                        else
//                        {
//                            _children.Add( a );
//                        }
//                    }
//                }
//            }
//        }
//        return success;


//        static bool MergeTranslations( IActivityMonitor monitor,
//                                       Dictionary<string, TranslationValue<IResourceContainer>> target,
//                                       Dictionary<string, TranslationValue<IResourceContainer>> above )
//        {
//            foreach( var kv in above )
//            {
//                var 
//            }
//        }
//    }

//}
