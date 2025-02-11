namespace CK.Core;

public sealed class FinalLocaleCultureSet
{
    readonly LocaleCultureSet _root;
    readonly bool _isPartialSet;

    /// <summary>
    /// Initializes a new empty final culture set.
    /// </summary>
    /// <param name="isPartialSet">
    /// True for the real final set of locales, false for an intermediate set of locale definitions that keeps the override definitions.
    /// </param>
    /// <param name="fullResourceName">
    /// Name of the fake resource in <see cref="EmptyResourceContainer.GeneratedCode"/>.
    /// This is used only for logging, there is no resource that originate directly from this container.
    /// </param>
    public FinalLocaleCultureSet( bool isPartialSet, string fullResourceName )
    {
        _root = new LocaleCultureSet( new Core.ResourceLocator( EmptyResourceContainer.GeneratedCode, fullResourceName ), NormalizedCultureInfo.CodeDefault );
        _isPartialSet = isPartialSet;
    }

    /// <summary>
    /// Updates this culture set with the content of <paramref name="newSet"/>.
    /// If the new set contains overrides, they apply and if they try to redefine an already defined
    /// resource, this is an error.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="newSet">The new set of resources to combine.</param>
    /// <returns>True on success, false on error.</returns>
    public bool Add( IActivityMonitor monitor, LocaleCultureSet newSet ) => _root.FinalMergeWith( monitor, newSet, _isPartialSet );

    /// <summary>
    /// Gets the root "en" set.
    /// <para>
    /// This set, as opposed to sets created by <see cref="ResourceContainerGlobalizationExtension.LoadLocales"/>
    /// is compact: parent cultures, even with no translation, always exist: if only "fr-FR" resources exist, the final
    /// root set will have a "fr" set with no translation that will contain the "fr-FR" in its <see cref="Children"/>.
    /// </para>
    /// <para>
    /// This must not be mutated directly, instead, <see cref="Add(IActivityMonitor, LocaleCultureSet)"/> must be called.
    /// </para>
    /// </summary>
    public LocaleCultureSet Root => _root;

    /// <summary>
    /// Removes the sets that don't have translation.
    /// <para>
    /// This can be used for final systems that handle culture fallbacks across the resources files.
    /// </para>
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
    /// <para>
    /// This must used for final systems that don't handle culture fallbacks across the resources files:
    /// each set must contain the whole set of key/value pairs.
    /// </para>
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
