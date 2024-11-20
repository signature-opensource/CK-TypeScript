namespace CK.Core;

public sealed class FinalLocaleCultureSet
{
    readonly LocaleCultureSet _root;

    internal FinalLocaleCultureSet( LocaleCultureSet final )
    {
        _root = final;
    }

    /// <summary>
    /// Gets the root "en" set.
    /// </summary>
    public LocaleCultureSet Root => _root;

    /// <summary>
    /// Removes the sets that don't have translation.
    /// <para>
    /// <see cref="LocaleCultureSet.Children"/> are lifted into the parent of the removed set.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    public void RemoveSetsWithEmptyTranslations( IActivityMonitor monitor )
    {
        var empties = _root.FlattenedAll.Where( s => !s.HasTranslations ).ToList();
        foreach( var s in empties )
        {
            _root.Remove( s );
            monitor.Trace( $"Removed LocaleCultureSet '{s.Culture.Name}' that has no translations." );
        }
    }

    /// <summary>
    /// Propagates the translations from the "en" root to all subordinated cultures.
    /// This must be used when resource lookup doesn't use fallbacks: each set must contain
    /// all the translations.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    public void PropagateFallbackTranslations( IActivityMonitor monitor )
    {
        int setCount = 0;
        int count = PropagateFallbackTranslations( _root, ref setCount );
        if( setCount > 0 )
        {
            monitor.Trace( $"Propagated {count} translations across {setCount} locale set(s)." );
        }

        static int PropagateFallbackTranslations( LocaleCultureSet s, ref int setCount )
        {
            int totalCount = 0;
            foreach( var sub in s.Children )
            {
                ++setCount;
                if( s.HasTranslations )
                {
                    foreach( var t in s.Translations )
                    {
                        if( sub.Translations.TryAdd( t.Key, t.Value ) )
                        {
                            ++totalCount;
                        }
                    }
                }
                totalCount += PropagateFallbackTranslations( sub, ref setCount );
            }
            return totalCount;
        }
    }

}
