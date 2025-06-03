using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.Localization;

/// <summary>
/// Imports the "@ngx-translate/core" library, adds automatic translations loader
/// based on <c>ck-gen/ts-locales/locales.ts</c>.
/// </summary>
[TypeScriptPackage]
[TypeScriptImportLibrary( "@ngx-translate/core", "^16", DependencyKind.Dependency )]

[NgProviderImport( "provideTranslateService, TranslateLoader", LibraryName = "@ngx-translate/core" )]
[NgProvider( "provideTranslateService( { defaultLanguage: 'fr', loader: { provide: TranslateLoader, useClass: CKTranslationsLoader } })" )]
[NgProviderImport( "CKTranslationsLoader", LibraryName = "@local/ck-gen/CK/Ng/Localization/TranslationsLoader" )]
[TypeScriptFile( "TranslationsLoader.ts", "CKTranslationsLoader" )]
public class TSPackage : TypeScriptPackage
{
}
