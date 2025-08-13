using CK.BinarySerialization;
using CK.Core;
using System.Text;

namespace CK.Setup;

public sealed partial class TypeScriptContext
{
    sealed class TypeScriptLocalesResourceHandler : LocalesResourceHandler
    {
        public TypeScriptLocalesResourceHandler( IResourceSpaceItemInstaller? installer,
                                                 ICoreDataCache packageDataCache,
                                                 ActiveCultureSet activeCultures,
                                                 bool sortKeys )
            : base( installer,
                    packageDataCache,
                    "ts-locales",
                    activeCultures,
                    installOption: sortKeys
                                    ? InstallOption.Full | InstallOption.WithSortedKeys
                                    : InstallOption.Full )
        {
        }

        protected override bool Install( IActivityMonitor monitor )
        {
            if( !base.Install( monitor ) ) return false;
            if( Installer != null )
            {
                using( Installer.PushSubPath( RootFolderName ) )
                {
                    var localesBody = new StringBuilder( """
                        export async function loadTranslations(lang: string): Promise<{[key: string]: string}> {
                            switch(lang) {

                        """ );
                    foreach( var c in ActiveCultures.AllActiveCultures )
                    {
                        if( !c.Culture.IsDefault )
                        {
                            localesBody.Append( "    case '" ).Append( c.Culture.Name ).Append( "': " )
                                        .Append( "return (await import('./" )
                                        .Append( c.Culture.Name )
                                        .Append( ".json')).default;" )
                                        .AppendLine();
                        }
                    }
                    localesBody.Append( """
                            default: return (await import('./en.json')).default;
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
