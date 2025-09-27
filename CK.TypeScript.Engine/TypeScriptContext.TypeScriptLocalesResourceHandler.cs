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
        readonly IReadOnlyCollection<VFeature> _features;
        readonly Dictionary<string, string> _angularLocaleImportByCultureName;
        readonly Dictionary<string, string> _zorroImportNameByCultureName;

        public TypeScriptLocalesResourceHandler( IResourceSpaceItemInstaller? installer,
                                                 ResCoreData resCoreData,
                                                 ActiveCultureSet activeCultures,
                                                 NormalizedCultureInfo defaultCulture,
                                                 bool sortKeys,
                                                 IReadOnlyCollection<VFeature> features )
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
            _features = features;
            _angularLocaleImportByCultureName = new Dictionary<string, string>()
            {
                // https://app.unpkg.com/@angular/common@20.2.3/files/locales
                { "de", "@angular/common/locales/de" },
                { "de-be", "@angular/common/locales/de-BE" },
                { "en-gb", "@angular/common/locales/en-GB" },
                { "es", "@angular/common/locales/es" },
                { "fr", "@angular/common/locales/fr" },
                { "fr-fr", "@angular/common/locales/fr" },
                { "fr-ca", "@angular/common/locales/fr-CA" },
                { "fr-be", "@angular/common/locales/fr-BE" },
                { "it", "@angular/common/locales/it" },
                { "nl", "@angular/common/locales/nl" },
                { "nl-be", "@angular/common/locales/nl-BE" },
                { "pt", "@angular/common/locales/pt" },
                { "uk", "@angular/common/locales/uk" },
                { "zh", "@angular/common/locales/zh" }
            };
            _zorroImportNameByCultureName = new Dictionary<string, string>()
            {
                // https://github.com/NG-ZORRO/ng-zorro-antd/tree/master/components/i18n/languages
                { "de", "de_DE" },
                { "en", "en_US" },
                { "en-gb", "en_GB" },
                { "es", "es_ES" },
                { "fr", "fr_FR" },
                { "fr-be", "fr_BE" },
                { "fr-ca", "fr_CA" },
                { "it", "it_IT" },
                { "nl", "nl_NL" },
                { "nl-be", "nl_BE" },
                { "pt", "pt_PT" },
                { "uk", "uk_UA" },
                { "zh", "zh_CN" },
                { "zh-hk", "zh_HK" },
                { "zh-tw", "zh_TW" }
            };
        }

        protected override bool Install( IActivityMonitor monitor )
        {
            if( !base.Install( monitor ) ) return false;
            if( Installer != null )
            {
                using( Installer.PushSubPath( RootFolderName ) )
                {
                    var localesBody = new StringBuilder();
                    bool isAngularProject = _features.Any( f => f.Name == "CK.TS.Angular" );
                    if( isAngularProject )
                    {
                        localesBody.Append( "import { inject, Injectable, LOCALE_ID, Provider, signal } from '@angular/core';" ).AppendLine();
                    }

                    bool hasNgLocalization = _resCoreData.PackageIndex.ContainsKey( "CK.Ng.Localization.LocalizationPackage" );
                    if( hasNgLocalization )
                    {
                        localesBody.AppendLine( "import { TranslateService } from '@ngx-translate/core';" );
                    }

                    bool hasNgZorro = _resCoreData.PackageIndex.ContainsKey( "CK.Ng.Zorro.ZorroPackage" );
                    if( hasNgZorro )
                    {
                        var zorroI18nImports = new StringBuilder( "{ NzI18nInterface, NzI18nService" );
                        foreach( var c in ActiveCultures.AllActiveCultures )
                        {
                            if( _zorroImportNameByCultureName.TryGetValue( c.Culture.Name, out var importName ) )
                            {
                                if( !zorroI18nImports.ToString().Contains( importName ) )
                                {
                                    zorroI18nImports.Append( $", {importName}" );
                                }
                            }
                        }
                        zorroI18nImports.Append( " }" );
                        localesBody.AppendLine( $"import {zorroI18nImports.ToString()} from 'ng-zorro-antd/i18n';" );
                    }
                    if( isAngularProject )
                    {
                        if( ActiveCultures.AllActiveCultures.Any( c => c.Culture.Name.ToLowerInvariant() != "en" && c.Culture.Name.ToLowerInvariant() != "en-us" ) )
                        {
                            localesBody.AppendLine( "import { registerLocaleData } from '@angular/common';" );
                        }

                        var importSB = new StringBuilder();
                        var registerSB = new StringBuilder();
                        foreach( var c in ActiveCultures.AllActiveCultures )
                        {
                            if( c.Culture.Name.ToLowerInvariant() != "en" && c.Culture.Name.ToLowerInvariant() != "en-us" )
                            {
                                if( _angularLocaleImportByCultureName.TryGetValue( c.Culture.Name, out var localeImportPath ) )
                                {
                                    if( importSB.ToString().Contains( localeImportPath ) ) continue;

                                    var importName = $"locale{ c.Culture.Name.Replace( "-", "" ).ToUpperInvariant()}";
                                    importSB.AppendLine( $"import {importName} from '{localeImportPath}';" );
                                    registerSB.AppendLine( $"registerLocaleData( {importName} );" );
                                }
                            }
                        }
                        localesBody.Append( importSB.ToString() ).AppendLine();
                        localesBody.Append( registerSB.ToString() ).AppendLine();
                    }

                    localesBody.Append( """
                        export async function loadTranslations( lang: string ): Promise<{ [key: string]: string }> {
                          switch( lang ) {

                        """ );
                    foreach( var c in ActiveCultures.AllActiveCultures )
                    {
                        if( c.Culture != _defaultCulture )
                        {
                            localesBody.Append( "    case '" ).Append( c.Culture.Name ).Append( "': " )
                                        .Append( "return ( await import( './" )
                                        .Append( c.Culture.Name )
                                        .Append( ".json' ) ).default;" )
                                        .AppendLine();
                        }
                    }

                    localesBody.Append( $$"""
                            default: return ( await import( './{{_defaultCulture.Name}}.json' ) ).default;
                          }
                        }

                        """ )
                        .AppendLine();

                    localesBody.Append( """
                        export type LocaleInfo = {
                          name: string;
                          nativeName: string;
                          englishName: string;
                          id: number;

                        """ );

                    if( hasNgLocalization )
                    {
                       localesBody.AppendLine( """
                          ngxTranslate: string;
                        """ );
                    }

                    if( hasNgZorro )
                    {
                       localesBody.AppendLine( """
                          zorro: NzI18nInterface;
                        """ );
                    }

                    localesBody.AppendLine( """
                        };

                        """ );

                    localesBody.Append( """
                        export type CKLocales = {
                          [localeCode: string]: LocaleInfo;
                        };

                        export const locales: CKLocales = {
                        
                        """ );

                    foreach( var c in ActiveCultures.AllActiveCultures )
                    {
                        localesBody.Append( "  \"" ).Append( c.Culture.Name )
                                    .Append( "\": { name: '" ).Append( c.Culture.Name )
                                    .Append( "', \"nativeName\": '" ).Append( c.Culture.Culture.NativeName )
                                    .Append( "', \"englishName\": '" ).Append( c.Culture.Culture.EnglishName )
                                    .Append( "', \"id\": " ).Append( c.Culture.Id );

                        if( hasNgLocalization )
                        {
                            localesBody.Append( ", \"ngxTranslate\": '" ).Append( c.Culture.Name ).Append( "'" );
                        }

                        if( hasNgZorro )
                        {
                            _zorroImportNameByCultureName.TryGetValue( c.Culture.Name, out var zorroImport );
                            if( zorroImport is null && c.Culture.Name.Contains( "-" ) )
                            {
                                var root = c.Culture.Name.Split( '-' )[0];
                                _zorroImportNameByCultureName.TryGetValue( root, out zorroImport );
                            }

                            if( zorroImport is null )
                            {
                                monitor.Warn( $"Unable to find NzI18nInterface associated to culture '{c.Culture.Name}'. The default 'en_US' has been associated instead." );
                            }
                            localesBody.Append( ", \"zorro\": " ).Append( zorroImport ?? "en_US" );
                        }

                        localesBody.Append( " }," ).AppendLine();
                    }
                    localesBody.Append( """
                        }

                        """ )
                        .AppendLine();

                    localesBody.AppendLine( $"export const DEFAULT_LOCALE_INFO = locales['{_defaultCulture.Name}'];" );

                    if( isAngularProject )
                    {
                        localesBody.Append( """
                            @Injectable( { providedIn: 'root' } )
                            export class LocaleService {
                              
                            """ );
                        if( hasNgLocalization )
                        {
                            localesBody.AppendLine( "readonly #translate = inject( TranslateService );" );
                        }
                        if( hasNgZorro )
                        {
                            localesBody.AppendLine( "  readonly #zorro = inject( NzI18nService );" );
                        }
                        localesBody.AppendLine()
                                    .AppendLine( $"  currentLocale = signal<string>( '{_defaultCulture.Name}' );" )
                                    .AppendLine();

                        localesBody.AppendLine( "  constructor() {" );
                        localesBody.Append( """
                                const supported = Object.keys( locales );
                            """ );
                        if( hasNgLocalization )
                        {
                            localesBody.AppendLine().Append( $"""
                                    this.#translate.addLangs( supported );
                                    this.#translate.setFallbackLang( '{_defaultCulture.Name}' );
                                """ );
                        }
                        localesBody.AppendLine().AppendLine( $"    this.setLocale( '{_defaultCulture.Name}' );" );
                        localesBody.AppendLine( "  }" );

                        localesBody.AppendLine().Append( """
                              async setLocale( locale: string ): Promise<void> {
                                const config = locales[locale];
                                if ( !config ) throw new Error( `Unsupported locale: ${locale}` );
                                
                                this.currentLocale.set( locale );
                            
                            """ );
                        if( hasNgLocalization )
                        {
                            localesBody.AppendLine( "    this.#translate.use( config.ngxTranslate );" );
                        }
                        if( hasNgZorro )
                        {
                            localesBody.AppendLine( "    this.#zorro.setLocale( config.zorro );" );
                        }
                        localesBody.AppendLine( "  }" );

                        localesBody.AppendLine( "}" );

                        localesBody.AppendLine().Append( """
                            export class LocaleId extends String {
                              readonly #localeService = inject( LocaleService );
                              constructor() {
                                super();
                              }

                              override toString(): string {
                                return this.#localeService.currentLocale();
                              }

                              override valueOf(): string {
                                return this.toString();
                              }
                            }

                            export const LocaleProvider: Provider = {
                              provide: LOCALE_ID,
                              useClass: LocaleId,
                              deps: [LocaleService],
                            };
                            """ );
                    }

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
