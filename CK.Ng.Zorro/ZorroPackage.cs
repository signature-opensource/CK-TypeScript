using CK.Core;
using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.Zorro;

[TypeScriptPackage]
[TypeScriptImportLibrary( "ng-zorro-antd", "~20.3", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "luxon", "^3", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@types/luxon", "^3", DependencyKind.DevDependency )]
[TypeScriptImportLibrary( "@angular/cdk", "^20", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@angular/animations", "^20", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@fortawesome/angular-fontawesome", "^3", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@fortawesome/fontawesome-svg-core", "^7", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@fortawesome/free-brands-svg-icons", "^7", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@fortawesome/free-regular-svg-icons", "^7", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@fortawesome/free-solid-svg-icons", "^7", DependencyKind.Dependency )]

[NgProviderImport( "provideNzI18n", From = "ng-zorro-antd/i18n" )]
[NgProviderImport( "DEFAULT_LOCALE_INFO", From = "@local/ck-gen/ts-locales/locales" )]
[NgProvider( "provideNzI18n( DEFAULT_LOCALE_INFO.zorro )" )]

[RegisterTypeScriptType( typeof( SimpleUserMessage ) )]
[RegisterTypeScriptType( typeof( UserMessageLevel ) )]
[TypeScriptFile( "date-helper.ts", "utcDateToLocal" )]
[TypeScriptFile( "datetime.pipe.ts", "DateFormatPipe" )]
[TypeScriptFile( "notification.service.ts", "NotificationService" )]
[TypeScriptFile( "responsive.directive.ts", "ResponsiveDirective" )]

[AppStyleImport( "ng-zorro-antd/ng-zorro-antd.less" )]
[Requires<CK.Ng.Localization.LocalizationPackage>]
public class ZorroPackage : TypeScriptPackage
{
}
