using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.Localization;

/// <summary>
/// Imports the "@ngx-translate/core" library, adds automatic translations loader
/// based on <c>ck-gen/ts-locales/locales.ts</c>.
/// </summary>
[TypeScriptPackage]
[TypeScriptImportLibrary( "@ngx-translate/core", "^17", DependencyKind.Dependency )]

[TypeScriptFile( "TranslationsLoader.ts", "CKTranslationsLoader" )]
[NgProviderImport( "provideTranslateService, TranslateLoader", From = "@ngx-translate/core" )]
[NgProviderImport( "CKTranslationsLoader", From = "@local/ck-gen" )]
[NgProviderImport( "DEFAULT_LOCALE_INFO", From = "@local/ck-gen/ts-locales/locales" )]
[NgProvider( "provideTranslateService( { fallbackLang: DEFAULT_LOCALE_INFO.ngxTranslate, lang: DEFAULT_LOCALE_INFO.ngxTranslate, loader: { provide: TranslateLoader, useClass: CKTranslationsLoader } })" )]
public class LocalizationPackage : TypeScriptPackage
{
}
