namespace CK.Core;

public sealed partial class LocaleCultureSet
{
    bool FinalMergeWith( IActivityMonitor monitor, LocaleCultureSet above )
    {
        bool success = MergeFinalTranslations( monitor, above._translations );
        if( above._children != null )
        {
            if( _children == null )
            {
                _children = above._children.Select( set => new LocaleCultureSet( set ) ).ToList();
            }
            else
            {
                foreach( var a in above._children )
                {
                    // If the culture is not knwon yet, ensures that it exists.
                    // This is not like in the regular processing: the final set is necessarily
                    // compact: parent cultures, even empty, always exist.
                    // This guaranties that if a subsequent set brings a "fr" when only "fr-FR" was
                    // previously handled, the tree is ready and we don"t have to move sets across
                    // Children lists.
                    var mine = Find( a._culture );
                    if( mine == null )
                    {
                        mine = this;
                        var fallbacks = a._culture.Fallbacks;
                        for( int i = fallbacks.Length - 1; i >= 0; --i )
                        {
                            var parent = fallbacks[i];
                            // Skip the "en" that is this root.
                            if( parent == _culture ) continue;
                            mine = EnsureDirectChild( mine, parent );
                        }
                        mine = EnsureDirectChild( mine, a._culture );
                    }
                    success &= mine.MergeFinalTranslations( monitor, a._translations );
                }
            }
        }
        return success;


        static LocaleCultureSet EnsureDirectChild( LocaleCultureSet mine, NormalizedCultureInfo c )
        {
            if( mine._children != null )
            {
                int idx = mine._children.IndexOf( s => s._culture == c );
                if( idx >= 0 )
                {
                    return mine._children[idx];
                }
            }
            else
            {
                mine._children = new List<LocaleCultureSet>();
            }
            var newOne = new LocaleCultureSet( new ResourceLocator( EmptyResourceContainer.GeneratedCode, "Automatic Parent Culture" ), c );
            mine._children.Add( newOne );
            return newOne;
        }
    }

    bool MergeFinalTranslations( IActivityMonitor monitor, Dictionary<string, TranslationValue>? above )
    {
        bool success = true;
        if( above != null && above.Count > 0 )
        {
            if( _translations == null || _translations.Count == 0 )
            {
                _translations = new Dictionary<string, TranslationValue>( above );
            }
            else
            {
                foreach( var (aKey, aValue) in above )
                {
                    if( _translations.TryGetValue( aKey, out var ourValue ) )
                    {
                        if( aValue.IsOverride )
                        {
                            if( ourValue.Text == aValue.Text )
                            {
                                monitor.Warn( $"""
                                               Useless override 'O:{aKey}' in {aValue.Origin}: it has the same value as the one from {ourValue.Origin}:
                                               {aValue.Text}
                                               """ );
                            }
                            else
                            {
                                // Override
                                _translations[aKey] = aValue;
                            }
                        }
                        else
                        {
                            monitor.Error( $"""
                                                Key '{aKey}' in {aValue.Origin} overides an existing value (it must be 'O:{aKey}'):
                                                This key is already defined by '{ourValue.Origin}' with the value:
                                                {ourValue.Text}
                                                The new value would be:
                                                {aValue.Text}
                                                """ );
                            success = false;
                        }
                    }
                    else
                    {
                        if( aValue.IsOverride )
                        {
                            monitor.Warn( $"Invalid override 'O:{aKey}' in {aValue.Origin}: the key doesn't exist, there's nothing to override." );
                        }
                        else
                        {
                            _translations.Add( aKey, aValue );
                        }
                    }
                }
            }
        }
        return success;
    }
}
