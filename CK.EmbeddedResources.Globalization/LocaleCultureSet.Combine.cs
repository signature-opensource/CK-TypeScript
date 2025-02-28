using CK.Core;
using System.Collections.Generic;
using System.Linq;

namespace CK.EmbeddedResources;

public sealed partial class LocaleCultureSet
{
    /// <summary>
    /// Updates this culture set with the content of <paramref name="above"/>.
    /// If this set contains overrides, they apply and if they try to redefine an existing
    /// resource in above, this is an error.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="above">The base resources.</param>
    /// <param name="isPartialSet">
    /// When true, a regular override that overrides nothing is kept as it may apply to the eventual set.
    /// When false, a regular override that overrides nothing is discarded and a warning is emitted.
    /// </param>
    /// <returns>True on success, false on error.</returns>
    internal bool Combine( IActivityMonitor monitor, LocaleCultureSet above, bool isPartialSet )
    {
        bool success = MergeFinalTranslations( monitor, above._translations, isPartialSet );
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
                    success &= mine.MergeFinalTranslations( monitor, a._translations, isPartialSet );
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

    bool MergeFinalTranslations( IActivityMonitor monitor, Dictionary<string, TranslationValue>? above, bool isPartialSet )
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
                        if( aValue.Override is ResourceOverrideKind.None )
                        {
                            monitor.Error( $"""
                                                Key '{aKey}' in {aValue.Origin} overides an existing value:
                                                This key is already defined by '{ourValue.Origin}' with the value:
                                                {ourValue.Text}
                                                The new value would be:
                                                {aValue.Text}
                                                Use Override prefix ('O:{aKey}') to allow this.
                                                """ );
                            success = false;
                        }
                        else
                        {
                            // Whether it is a "O", "?O" or "!O" we don't care here as we override.
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
                    }
                    else
                    {
                        // No existing translation defined.
                        // - If this is a partial set, we keep the translation as-is whatever Override it is: the Override will
                        //   be handled while merging in the final set.
                        // - Of course, if the resource is a normal one it is added.
                        // - And if it is a "!O": the resource is always added (as it always overrides).
                        if( isPartialSet
                            || aValue.Override is ResourceOverrideKind.None or ResourceOverrideKind.Always )
                        {
                            _translations.Add( aKey, aValue );
                        }
                        else
                        {
                            Throw.DebugAssert( aValue.Override is ResourceOverrideKind.Regular or ResourceOverrideKind.Optional );
                            if( aValue.Override is ResourceOverrideKind.Regular )
                            {
                                monitor.Warn( $"Invalid override 'O:{aKey}' in {aValue.Origin}: the key doesn't exist, there's nothing to override." );
                            }
                            // ResourceOverrideKind.Optional doesn't add the resource and doesn't warn
                            // since it doesn't already exist. 
                        }
                    }
                }
            }
        }
        return success;
    }
}
