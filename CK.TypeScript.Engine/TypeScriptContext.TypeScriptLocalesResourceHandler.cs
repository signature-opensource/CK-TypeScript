using CK.BinarySerialization;
using CK.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup;

public sealed partial class TypeScriptContext
{
    sealed class TypeScriptLocalesResourceHandler : LocalesResourceHandler
    {
        readonly ResCoreData _resCoreData;
        readonly NormalizedCultureInfo _defaultCulture;

        public TypeScriptLocalesResourceHandler( IResourceSpaceItemInstaller? installer,
                                                 ResCoreData resCoreData,
                                                 ActiveCultureSet activeCultures,
                                                 NormalizedCultureInfo defaultCulture,
                                                 bool sortKeys )
            : base( installer,
                    resCoreData.SpaceDataCache,
                    "ts-locales",
                    activeCultures,
                    installOption: sortKeys
                                    ? InstallOption.Full | InstallOption.WithSortedKeys
                                    : InstallOption.Full )
        {
            _resCoreData = resCoreData;
            _defaultCulture = defaultCulture;
        }

        protected override bool Install( IActivityMonitor monitor )
        {
            if( !base.Install( monitor ) ) return false;
            if( Installer != null )
            {
                using( Installer.PushSubPath( RootFolderName ) )
                {
                    bool hasAngular = _resCoreData.PackageIndex.ContainsKey( "CK.TS.Angular" );
                    bool hasNgLocalization = _resCoreData.PackageIndex.ContainsKey( "CK.Ng.Localization" );
                    bool hasNgZorro = _resCoreData.PackageIndex.ContainsKey( "CK.Ng.Zorro" );

                    var localesBody = new StringBuilder( """
                        export async function loadTranslations(lang: string): Promise<{[key: string]: string}> {
                            switch(lang) {

                        """ );
                    foreach( var c in ActiveCultures.AllActiveCultures )
                    {
                        if( c.Culture != _defaultCulture )
                        {
                            localesBody.Append( "    case '" ).Append( c.Culture.Name ).Append( "': " )
                                        .Append( "return (await import('./" )
                                        .Append( c.Culture.Name )
                                        .Append( ".json')).default;" )
                                        .AppendLine();
                        }
                    }
                    localesBody.Append( $$"""
                            default: return (await import('./{{_defaultCulture.Name}}.json')).default;
                          }
                        }

                        """ );

                    localesBody.Append( """
                        export type LocaleInfo = {
                          name: string;
                          nativeName: string;
                          englishName: string;
                          id: number;
                        };

                        export type CKLocales = {
                          [localeCode: string]: LocaleInfo;
                        };

                        """ );

                    localesBody.Append( """

                        export const locales: CKLocales = {

                        """ );

                    foreach( var c in ActiveCultures.AllActiveCultures )
                    {
                        localesBody.Append( "  \"" ).Append( c.Culture.Name )
                                    .Append( "\": { name: '" ).Append( c.Culture.Name )
                                    .Append( "', \"nativeName\": '" ).Append( c.Culture.Culture.NativeName )
                                    .Append( "', \"englishName\": '" ).Append( c.Culture.Culture.EnglishName )
                                    .Append( "', \"id\": " ).Append( c.Culture.Id )
                                    .Append( " }," )
                                    .AppendLine();
                    }
                    localesBody.Append( """
                        }

                        """ );

                    Installer.Write( $"locales.ts", localesBody.ToString() );
                }
            }
            return true;
        }

        /// <summary>
        /// Direct relay to <see cref="LocalesResourceHandler.ReadLiveState(IActivityMonitor, ResCoreData, IBinaryDeserializer)"/>:
        /// there is no specific live behavior since adding or removing an active culture cannot be done dynamically.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="spaceData">The deserialized resource space data.</param>
        /// <param name="d">The deserializer for the primary <see cref="ResSpace.LiveStateFileName"/>.</param>
        /// <returns>The live updater on success, null on error. Errors are logged.</returns>
        public static new ILiveUpdater? ReadLiveState( IActivityMonitor monitor, ResCoreData spaceData, IBinaryDeserializer d )
        {
            return LocalesResourceHandler.ReadLiveState( monitor, spaceData, d );
        }

    }
}
