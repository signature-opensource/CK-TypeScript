using CK.Core;
using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.Zorro;

[TypeScriptPackage]
[TypeScriptImportLibrary( "ng-zorro-antd", "^19", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "luxon", "^3", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@types/luxon", "^3", DependencyKind.DevDependency, ForceUse = true )]
[TypeScriptImportLibrary( "@angular/cdk", "^19", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@angular/animations", "^19", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@fortawesome/angular-fontawesome", "^1", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@fortawesome/fontawesome-svg-core", "^6", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@fortawesome/free-brands-svg-icons", "^6", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@fortawesome/free-regular-svg-icons", "^6", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@fortawesome/free-solid-svg-icons", "^6", DependencyKind.Dependency, ForceUse = true )]


[NgProviderImport( "fr_FR, provideNzI18n", LibraryName = "ng-zorro-antd/i18n" )]
[NgProvider( "provideNzI18n( fr_FR )" )]
[NgProviderImport( "provideAnimationsAsync", LibraryName = "@angular/platform-browser/animations/async" )]
[NgProvider( "provideAnimationsAsync()" )]

[RegisterTypeScriptType( typeof( SimpleUserMessage ) )]
[RegisterTypeScriptType( typeof( UserMessageLevel ) )]

[AppStyleImport( "ng-zorro-antd/ng-zorro-antd.less" )]
[Requires<CK.Ng.Localization.TSPackage>]
public class TSPackage : TypeScriptPackage
{
}
