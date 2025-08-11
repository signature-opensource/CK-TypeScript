using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.Localization;

/// <summary>
/// Imports the "@ngx-translate/core" library, adds automatic translations loader
/// based on <c>ck-gen/ts-locales/locales.ts</c>.
/// </summary>
[TypeScriptPackage]
[TypeScriptImportLibrary( "@ngx-translate/core", "^16", DependencyKind.Dependency )]

[TypeScriptFile( "TranslationsLoader.ts", "CKTranslationsLoader" )]
[NgProviderImport( "provideTranslateService, TranslateLoader", From = "@ngx-translate/core" )]
[NgProviderImport( "CKTranslationsLoader", From = "@local/ck-gen" )]
[NgProvider( "provideTranslateService( { defaultLanguage: 'fr', loader: { provide: TranslateLoader, useClass: CKTranslationsLoader } })" )]
public class TSPackage : TypeScriptPackage
{
}
