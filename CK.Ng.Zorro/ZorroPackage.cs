using CK.Core;
using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.Zorro;

[TypeScriptPackage]
[TypeScriptImportLibrary( "ng-zorro-antd", "^19", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "luxon", "^3", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@types/luxon", "^3", DependencyKind.DevDependency )]
[TypeScriptImportLibrary( "@angular/cdk", "^19", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@angular/animations", "^19", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@fortawesome/angular-fontawesome", "^1", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@fortawesome/fontawesome-svg-core", "^6", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@fortawesome/free-brands-svg-icons", "^6", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@fortawesome/free-regular-svg-icons", "^6", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@fortawesome/free-solid-svg-icons", "^6", DependencyKind.Dependency )]


[NgProviderImport( "fr_FR, provideNzI18n", From = "ng-zorro-antd/i18n" )]
[NgProvider( "provideNzI18n( fr_FR )" )]
[NgProviderImport( "provideAnimationsAsync", From = "@angular/platform-browser/animations/async" )]
[NgProvider( "provideAnimationsAsync()" )]

[RegisterTypeScriptType( typeof( SimpleUserMessage ) )]
[RegisterTypeScriptType( typeof( UserMessageLevel ) )]
[TypeScriptFile( "date-helper.ts", "utcDateToLocal" )]
[TypeScriptFile( "datetime.pipe.ts", "DateFormatPipe" )]
[TypeScriptFile( "notification.service.ts", "CKNotificationService" )]
[TypeScriptFile( "responsive.directive.ts", "ResponsiveDirective" )]

[AppStyleImport( "ng-zorro-antd/ng-zorro-antd.less" )]
[Requires<CK.Ng.Localization.TSPackage>]
public class ZorroPackage : TypeScriptPackage
{
}
