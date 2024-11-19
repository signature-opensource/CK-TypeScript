using CK.Core;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System;
using System.Linq;
using System.Text.Json;

namespace CK.Core;

public static class ResourceContainerGlobalizationExtension
{
    /// <summary>
    /// Processes the <paramref name="folder"/> if it exists and returns a <see cref="LocaleCultureSet"/>.
    /// Returns false on error (error has been logged).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="container">The container of resources.</param>
    /// <param name="activeCultures">The cultures to consider. Cultures not in this set are skipped.</param>
    /// <param name="locales">The loaded locales. Can be null on success if no "<paramref name="folder"/>/" exists.</param>
    /// <returns>True on success, false on error.</returns>
    public static bool LoadLocales( this IResourceContainer container,
                                    IActivityMonitor monitor,
                                    IReadOnlySet<NormalizedCultureInfo> activeCultures,
                                    out LocaleCultureSet? locales,
                                    string folder = "locales" )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( folder );
        var content = container.GetFileProvider().GetDirectoryContents( folder );
        if( content.Exists )
        {
            using( monitor.OpenInfo( $"Reading '{folder}/' folder from '{container}'." ) )
            {
                var defFilePath = $"{folder}/default.json";
                if( !container.TryGetResource( defFilePath, out var defFile )
                    && !container.TryGetResource( defFilePath + 'c', out defFile ) )
                {
                    monitor.Error( $"Missing '{defFilePath}' file in {container}. This file must contain all the resources in english." );
                }
                else if( ReadJson( monitor, defFile, out var defTranslations ) )
                {
                    var defaultSet = new LocaleCultureSet( defFile, NormalizedCultureInfo.CodeDefault, defTranslations );
                    locales = ReadLocales( monitor, container, defaultSet, container.GetAllResourceLocatorsFrom( content ), activeCultures );
                    if( locales == null ) return false;
                }

            }
        }
        locales = null;
        return true;
    }

    static LocaleCultureSet? ReadLocales( IActivityMonitor monitor,
                                          IResourceContainer container,
                                          LocaleCultureSet defaultSet,
                                          IEnumerable<ResourceLocator> allResources,
                                          IReadOnlySet<NormalizedCultureInfo> activeCultures )
    {
        bool success = true;

        // Should we use the IFileProvider or AllResources?
        // Missing a container.GetAllResourcesFrom( IDirectoryContents ) when we don't care about folders and want a
        // direct access to the items.
        var candidates = allResources.Where( r => r != defaultSet.Origin
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

        static bool ReadSpecificSet( IActivityMonitor monitor,
                                        Core.ResourceLocator locator,
                                        NormalizedCultureInfo culture,
                                        LocaleCultureSet defaultSet,
                                        [NotNullWhen( true )] out LocaleCultureSet? specificSet )
        {
            specificSet = null;
            if( ReadJson( monitor, locator, out var content ) )
            {
                bool success = true;
                foreach( var kv in content )
                {
                    // When a key overrides/masks an entry, it masks a key defined in another component.
                    // The fact that "fr-FR" overrides the "Super.EvenBetter.Text" key does NOT mean that
                    // this key must also be defined as an override in the "default.json" file: it has to be defined
                    // in the final merged set by a lower-level component.
                    if( !kv.Value.IsOverride && !defaultSet.Translations.ContainsKey( kv.Key ) )
                    {
                        monitor.Error( $"""
                                        Missing default key in default translation file.
                                        Key '{kv.Value}' defined in translation file '{locator}' doesn't exist in '{locator.LocalResourceName}'.
                                        """ );
                        success = false;
                    }
                }
                if( success )
                {
                    specificSet = new LocaleCultureSet( locator, culture, content );
                    return true;
                }
            }
            return false;
        }
    }


    static bool ReadJson( IActivityMonitor monitor, Core.ResourceLocator locator, [NotNullWhen( true )] out Dictionary<string, TranslationValue>? content )
    {
        content = null;
        try
        {
            using var s = locator.GetStream();
            content = ReadJsonTranslationFile( locator.Container, s, skipComments: locator.ResourceName.EndsWith( ".jsonc" ) );
            return true;
        }
        catch( Exception ex )
        {
            monitor.Error( $"Unable to read translations from '{locator}'.", ex );
            return false;
        }
    }

    static Dictionary<string, TranslationValue> ReadJsonTranslationFile( IResourceContainer origin, Stream s, bool skipComments )
    {
        var options = new JsonReaderOptions
        {
            CommentHandling = skipComments ? JsonCommentHandling.Skip : JsonCommentHandling.Disallow,
            AllowTrailingCommas = true
        };
        using var context = Utf8JsonStreamReader.Create( s, options, out var reader );
        var result = new Dictionary<string, TranslationValue>();
        ReadJson( ref reader, context, origin, result );
        return result;

        static void ReadJson( ref Utf8JsonReader r, IUtf8JsonReaderContext context, IResourceContainer origin, Dictionary<string, TranslationValue> target )
        {
            if( r.TokenType == JsonTokenType.None && !r.Read() )
            {
                Throw.InvalidDataException( $"Expected a json object." );
            }
            Throw.CheckData( r.TokenType == JsonTokenType.StartObject );
            ReadObject( ref r, context, origin, target, "" );

            static void ReadObject( ref Utf8JsonReader r,
                                    IUtf8JsonReaderContext context,
                                    IResourceContainer origin,
                                    Dictionary<string, TranslationValue> target,
                                    string parentPath )
            {
                Throw.DebugAssert( r.TokenType == JsonTokenType.StartObject );
                Throw.DebugAssert( parentPath.Length == 0 || parentPath[^1] == '.' );

                r.ReadWithMoreData( context );
                while( r.TokenType == JsonTokenType.PropertyName )
                {
                    var propertyName = r.GetString();
                    Throw.CheckData( "Expected non empty property name.", !string.IsNullOrWhiteSpace( propertyName ) );
                    Throw.CheckData( "Property name cannot end or start with '.' nor contain '..'.",
                                     propertyName[0] != '.' && propertyName[^1] != '.' && !propertyName.Contains( ".." ) );
                    bool isOverride = false;
                    if( propertyName.StartsWith( "O:" ) )
                    {
                        isOverride = true;
                        propertyName = propertyName.Substring( 2 );
                    }
                    propertyName = parentPath + propertyName;
                    r.ReadWithMoreData( context );
                    if( r.TokenType == JsonTokenType.StartObject )
                    {
                        if( isOverride )
                        {
                            Throw.InvalidDataException( $"Invalid parent property name \"O:{propertyName}\": When defining a subordinated object, the parent key must not be an override." );
                        }
                        ReadObject( ref r, context, origin, target, parentPath + propertyName + '.' );
                    }
                    else
                    {
                        if( r.TokenType != JsonTokenType.String )
                        {
                            Throw.InvalidDataException( $"Expected a string or an object, got a '{r.TokenType}'." );
                        }
                        if( !target.TryAdd( propertyName, new TranslationValue( r.GetString()!, origin, isOverride ) ) )
                        {
                            Throw.InvalidDataException( $"Duplicate key '{propertyName}' found." );
                        }
                    }
                    r.ReadWithMoreData( context );
                }
            }
        }
    }

}

