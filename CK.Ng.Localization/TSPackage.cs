using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.Localization;

[TypeScriptPackage]
[TypeScriptImportLibrary( "@ngx-translate/core", "^16", DependencyKind.Dependency )]
[NgProviderImport( "LOCALE_ID", LibraryName = "@angular/core" )]
[NgProvider( "{ provide: LOCALE_ID, useValue: 'fr-FR' }" )]
public class TSPackage
{
    
}
