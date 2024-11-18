using CK.Core;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System;
using System.Linq;

namespace CK.TypeScript.Engine;

sealed partial class TSLocaleCultureSet
{
    /// <summary>
    /// Loads the "ts-locales/" folder if it exists.
    /// Returns false on error (error has been logged).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="container">The container of resources.</param>
    /// <param name="activeCultures">The cultures to consider.</param>
    /// <param name="locales">The loaded locales. Can be null on success if no "ts-locales/" exists.</param>
    /// <returns>True on success, false on error.</returns>
    public static bool LoadTSLocales( IActivityMonitor monitor,
                                      IResourceContainer container,
                                      IReadOnlySet<NormalizedCultureInfo> activeCultures,
                                      out TSLocaleCultureSet? locales )
    {
        if( container.HasDirectory( "ts-locales" ) )
        {
            using( monitor.OpenInfo( $"Reading 'ts-locales/' folder from '{container}'." ) )
            {
                locales = ReadTSLocales( monitor, container, activeCultures );
                if( locales == null ) return false;
            }
        }
        locales = null;
        return true;
    }

    static TSLocaleCultureSet? ReadTSLocales( IActivityMonitor monitor, IResourceContainer container, IReadOnlySet<NormalizedCultureInfo> activeCultures )
    {
        if( !ReadDefaultSet( monitor, container, out var defaultSet ) )
        {
            return null;
        }

        bool success = true;

        // Should we use the IFileProvider or AllResources?
        // Missing a container.GetAllResourcesFrom( IDirectoryContents ) when we don't care about folders.
        var candidates = container.AllResources.Where( r => r != defaultSet.Origin
                                                            && r.LocalResourceName.Span.StartsWith( "ts-locales/" )
                                                            && (r.LocalResourceName.Span.EndsWith( ".json" ) || r.LocalResourceName.Span.EndsWith( ".jsonc" )) );

        var others = candidates.OrderBy( o => o.ResourceName.Length );
        foreach( var o in others )
        {
            var cName = Path.GetFileNameWithoutExtension( o.ResourceName.AsSpan().ToString() );
            if( !NormalizedCultureInfo.IsValidCultureName( cName ) )
            {
                monitor.Error( $"Invalid '{o}'. Name '{cName}' is not a BCP47 compliant culture name." );
                success = false;
            }
            else
            {
                var c = NormalizedCultureInfo.FindNormalizedCultureInfo( cName );
                if( c == null || !activeCultures.Contains( c ) )
                {
                    monitor.Warn( $"Ignoring translation file for '{cName}' as it doesn't appear in the TSBinPathConfiguration.ActiveCultures list." );
                }
                else if( c == NormalizedCultureInfo.CodeDefault )
                {
                    monitor.Error( $"File '{o}' defines the \"en\" culture. This is the default culture that must be in 'default.json' (or 'default.jsonc') file." );
                    success = false;
                }
                else
                {
                    if( !c.IsNeutralCulture && defaultSet.Find( c.NeutralCulture ) == null )
                    {
                        monitor.Error( $"""
                                        Cannot handle culture '{cName}' from '{o}'.
                                        The translation file for the neutral culture '{c.NeutralCulture.Name}' is required.
                                        """ );
                        success = false;
                    }
                    else
                    {
                        var parent = c.IsNeutralCulture ? defaultSet : defaultSet.FindClosest( c );
                        Throw.DebugAssert( "Since the NeutralCulture exists.", parent != null );
                        if( parent.Culture == c )
                        {
                            monitor.Error( $"Duplicate files found for culture '{cName}': '{o}' and '{parent.Origin}' lead to the same culture." );
                            success = false;
                        }
                        else
                        {
                            if( ReadSpecificSet( monitor, o, c, defaultSet, out var specificSet ) )
                            {
                                parent.AddSpecific( specificSet );
                                monitor.Trace( $"Successfully loaded {specificSet.Translations.Count} translations from '{o.ResourceName}'." );
                            }
                            else
                            {
                                success = false;
                            }
                        }
                    }
                }
            }
        }
        return success ? defaultSet : null;

        static bool ReadDefaultSet( IActivityMonitor monitor,
                                    IResourceContainer container,
                                    [NotNullWhen( true )] out TSLocaleCultureSet? defaultSet )
        {
            defaultSet = null;
            if( !container.TryGetResource( "ts-locales/default.json", out var defFile )
                && !container.TryGetResource( "ts-locales/default.jsonc", out defFile ) )
            {
                monitor.Error( $"Missing 'ts-locales/default.json' file in {container}. This file must contain all the resources in english." );
                return false;
            }
            if( !ReadJson( monitor, defFile, out var defTranslations ) )
            {
                return false;
            }
            defaultSet = new TSLocaleCultureSet( defFile, NormalizedCultureInfo.CodeDefault, defTranslations );
            return true;
        }

        static bool ReadSpecificSet( IActivityMonitor monitor,
                                        Core.ResourceLocator locator,
                                        NormalizedCultureInfo culture,
                                        TSLocaleCultureSet defaultSet,
                                        [NotNullWhen( true )] out TSLocaleCultureSet? specificSet )
        {
            specificSet = null;
            if( ReadJson( monitor, locator, out var content ) )
            {
                bool success = true;
                foreach( var k in content.Keys )
                {
                    if( !defaultSet.Translations.ContainsKey( k ) )
                    {
                        monitor.Error( $"""
                                        Missing default key in default translation file.
                                        Key '{k}' defined in translation file '{locator}' doesn't exist in '{locator.LocalResourceName}'.
                                        """ );
                        success = false;
                    }
                }
                if( success )
                {
                    specificSet = new TSLocaleCultureSet( locator, culture, content );
                    return true;
                }
            }
            return false;
        }

        static bool ReadJson( IActivityMonitor monitor, Core.ResourceLocator locator, [NotNullWhen( true )] out Dictionary<string, string>? content )
        {
            content = null;
            try
            {
                using var s = locator.GetStream();
                content = GlobalizationFileHelper.ReadJsonTranslationFile( s, skipComments: locator.ResourceName.EndsWith( ".jsonc" ) );
                return true;
            }
            catch( Exception ex )
            {
                monitor.Error( $"Unable to read translations from '{locator}'.", ex );
                return false;
            }
        }
    }


}
