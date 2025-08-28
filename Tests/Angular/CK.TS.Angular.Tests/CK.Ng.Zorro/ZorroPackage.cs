using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.Zorro;

/// <summary>
/// Minimal implementation of the real CK.Ng.Zorro.ZorroPackage. 
/// </summary>
[TypeScriptPackage]
[TypeScriptImportLibrary( "ng-zorro-antd", "^20", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@fortawesome/angular-fontawesome", "^3", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@angular/animations", "^20", DependencyKind.Dependency )]
[NgProviderImport( "provideAnimationsAsync", From = "@angular/platform-browser/animations/async" )]
[NgProvider( "provideAnimationsAsync()" )]
[AppStyleImport( "ng-zorro-antd/ng-zorro-antd.less" )]
public class ZorroPackage : TypeScriptPackage
{
}
