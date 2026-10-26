using CK.TypeScript;

namespace CK.Ng.Zorro;

/// <summary>
/// Minimal implementation of the real CK.Ng.Zorro.ZorroPackage. 
/// </summary>
[TypeScriptPackage]
[TypeScriptImportLibrary( "ng-zorro-antd", "^22", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@angular/cdk", "^22", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@fortawesome/angular-fontawesome", "^5", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@fortawesome/fontawesome-svg-core", "^7", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@angular/animations", "^22", DependencyKind.Dependency )]
[AppStyleImport( "ng-zorro-antd/ng-zorro-antd.less" )]
public class ZorroTestPackage : TypeScriptPackage
{
}
