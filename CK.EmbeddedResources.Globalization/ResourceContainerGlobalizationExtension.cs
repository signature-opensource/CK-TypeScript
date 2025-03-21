using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CK.Core;

public static class ResourceContainerGlobalizationExtension
{
    /// <summary>
    /// Processes the <paramref name="folder"/> if it exists and returns a <see cref="LocaleCultureSet"/>.
    /// Returns false on error (error has been logged).
    /// <para>
    /// The following folder:
    /// <code>
    /// kind-locales/
    ///   default.jsonc
    ///   fr-FR.json
    ///   en/
    ///     en-GB.jsonc
    ///   en-US.jsonc
    ///   de.json
    /// </code>
    /// Will produce the "en" (from the <c>default.jsonc</c> file), the "en-GB", "en-US", "fr-FR" and "de" culture set.
    /// Folders are ignored, files with ".jsonc" can contain comments whereas in ".json" files, comments are prohibited.
    /// Parent cultures are not created if not needed: here the "fr" set is not created.
    /// </para>
    /// <para>
    /// The the <c>default.jsonc</c> file is required unless <paramref name="isOverrideFolder"/> is true.
    /// </para>
    /// <para>
    /// When <paramref name="isOverrideFolder"/> is true, the loaded folder is an override that doesn't define new resources.
    /// It doesn't need a <c>default.json</c> and all resources in its files are considered to have an implicit "O:" key prefix.
    /// 
    /// </para>
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
                                    string folder = "locales",
                                    bool isOverrideFolder = false )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( folder );
        if( container.TryGetFolder( folder, out var content ) )
        {
            using( monitor.OpenInfo( $"Reading {content}." ) )
            {
                var defaultSet = CreateRoot( monitor, content, isOverrideFolder );
                if( defaultSet != null )
                {
                    locales = ReadLocales( monitor, defaultSet, isOverrideFolder, content.AllResources, activeCultures );
                    return locales != null;
                }
            }
        }
        locales = null;
        return true;

        static LocaleCultureSet? CreateRoot( IActivityMonitor monitor, ResourceFolder folder, bool isOverrideFolder )
        {
            if( folder.TryGetResource( "default.json", out var defFile )
                || folder.TryGetResource( "default.jsonc", out defFile )
                || isOverrideFolder && (folder.TryGetResource( "en.json", out defFile ) || folder.TryGetResource( "en.jsonc", out defFile )) )
            {
                if( ReadJson( monitor, defFile, isOverrideFolder, out var defTranslations ) )
                {
                    return new LocaleCultureSet( defFile, NormalizedCultureInfo.CodeDefault, defTranslations );
                }
            }
            else
            {
                // We didn't find a default (or an "en" if isOverrideFolder is true).
                if( isOverrideFolder )
                {
                    // If isOverrideFolder, simply returns an empty "en" root set.
                    return new LocaleCultureSet( defFile, NormalizedCultureInfo.CodeDefault );
                }
                // Regular folder MUST contain a "default".
                monitor.Error( $"Missing 'default.json' file in {folder}. This file must contain all the resources in english." );
            }
            return null;
        }

        static LocaleCultureSet? ReadLocales( IActivityMonitor monitor,
                                              LocaleCultureSet defaultSet,
                                              bool isOverrideFolder,
                                              IEnumerable<ResourceLocator> allResources,
                                              IReadOnlySet<NormalizedCultureInfo> activeCultures )
        {
            bool success = true;

            // Ordering by increasing length: process less specific first.
            var others = allResources.Where( r => r != defaultSet.Origin
                                                  && (r.LocalResourceName.Span.EndsWith( ".json" ) || r.LocalResourceName.Span.EndsWith( ".jsonc" )) )
                                     .OrderBy( o => o.ResourceName.Length );

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
                        //
                        // When isOverrideFolder is true, finding an "en" here means there were a "default.json" (because of the r != defaultSet.Origin
                        // condition in the others): the error can be the same as with a false isOverrideFolder.
                        //
                        monitor.Error( $"File {o} defines the \"en\" culture. This is the default culture that must be in 'default.json' (or 'default.jsonc') file." );
                        success = false;
                    }
                    else
                    {
                        if( !c.IsNeutralCulture && defaultSet.Find( c.NeutralCulture ) == null )
                        {
                            monitor.Error( $"""
                                        Cannot handle culture '{cName}' from {o}.
                                        The translation file for the neutral culture '{c.NeutralCulture.Name}' is required.
                                        """ );
                            success = false;
                        }
                        else
                        {
                            var parent = c.IsNeutralCulture || c.IsDefault
                                            ? defaultSet
                                            : defaultSet.FindClosest( c );
                            Throw.DebugAssert( "Since the NeutralCulture exists.", parent != null );
                            if( parent.Culture == c )
                            {
                                monitor.Error( $"Duplicate files found for culture '{cName}': {o} and {parent.Origin} lead to the same culture." );
                                success = false;
                            }
                            else
                            {
                                if( ReadSpecificSet( monitor, o, c, defaultSet, isOverrideFolder, out var specificSet ) )
                                {
                                    parent.AddSpecific( specificSet );
                                    monitor.Trace( $"Successfully loaded {specificSet.Translations.Count} translations from {o}." );
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
                                         bool isOverrideFolder,
                                         [NotNullWhen( true )] out LocaleCultureSet? specificSet )
            {
                specificSet = null;
                if( ReadJson( monitor, locator, isOverrideFolder, out var content ) )
                {
                    bool success = true;
                    foreach( var kv in content )
                    {
                        // When a key overrides/masks an entry, it masks a key defined in another component.
                        // The fact that "fr-FR" overrides the "Super.EvenBetter.Text" key does NOT mean that
                        // this key must also be defined as an override in the "default.json" file: it has to be defined
                        // in the final merged set by a lower-level component. Override handling is done by the FinalSet,
                        // not here.
                        if( !kv.Value.IsOverride && !defaultSet.Translations.ContainsKey( kv.Key ) )
                        {
                            monitor.Error( $"""
                                        Missing key in default translation file.
                                        Key '{kv.Value}' defined in translation file {locator} doesn't exist in {defaultSet.Origin}.
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


        static bool ReadJson( IActivityMonitor monitor,
                              ResourceLocator locator,
                              bool isOverrideFolder,
                              [NotNullWhen( true )] out Dictionary<string, TranslationValue>? content )
        {
            content = null;
            try
            {
                using var s = locator.GetStream();
                content = ReadJsonTranslationFile( locator, s, isOverrideFolder, skipComments: locator.ResourceName.EndsWith( ".jsonc" ) );
                return true;
            }
            catch( Exception ex )
            {
                monitor.Error( $"Unable to read translations from {locator}.", ex );
                return false;
            }
        }

        static Dictionary<string, TranslationValue> ReadJsonTranslationFile( ResourceLocator origin, Stream s, bool isOverrideFolder, bool skipComments )
        {
            var options = new JsonReaderOptions
            {
                CommentHandling = skipComments ? JsonCommentHandling.Skip : JsonCommentHandling.Disallow,
                AllowTrailingCommas = true
            };
            using var context = Utf8JsonStreamReaderContext.Create( s, options, out var reader );
            var result = new Dictionary<string, TranslationValue>();
            ReadJson( ref reader, context, origin, isOverrideFolder, result );
            return result;

            static void ReadJson( ref Utf8JsonReader r,
                                  IUtf8JsonReaderContext context,
                                  ResourceLocator origin,
                                  bool isOverrideFolder,
                                  Dictionary<string, TranslationValue> target )
            {
                if( r.TokenType == JsonTokenType.None && !r.Read() )
                {
                    Throw.InvalidDataException( $"Expected a json object." );
                }
                Throw.CheckData( r.TokenType == JsonTokenType.StartObject );
                ReadObject( ref r, context, origin, isOverrideFolder, target, "" );

                static void ReadObject( ref Utf8JsonReader r,
                                        IUtf8JsonReaderContext context,
                                        ResourceLocator origin,
                                        bool isOverrideFolder,
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
                        bool isOverride = isOverrideFolder;
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
                            ReadObject( ref r, context, origin, isOverrideFolder, target, parentPath + propertyName + '.' );
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
}
