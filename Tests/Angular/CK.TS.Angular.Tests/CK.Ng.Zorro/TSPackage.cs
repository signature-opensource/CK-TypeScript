using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.Zorro;

[TypeScriptPackage]
[TypeScriptImportLibrary( "ng-zorro-antd", "^19", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@fortawesome/angular-fontawesome", "^1", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@angular/animations", "^19", DependencyKind.Dependency, ForceUse = true )]
[NgProviderImport( "provideAnimationsAsync", From = "@angular/platform-browser/animations/async" )]
[NgProvider( "provideAnimationsAsync()" )]
[AppStyleImport( "ng-zorro-antd/ng-zorro-antd.less" )]
public class TSPackage : TypeScriptPackage
{
}
