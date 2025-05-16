using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.Zorro;

[TypeScriptPackage]
[TypeScriptImportLibrary( "ng-zorro-antd", "^19", DependencyKind.Dependency, ForceUse = true )]
[NgProviderImport( "fr_FR, NZ_I18N", LibraryName = "ng-zorro-antd/i18n" )]
[NgProvider( "{ provide: NZ_I18N, useValue: fr_FR }" )]
[NgAppStyleImport( "ng-zorro-antd/ng-zorro-antd.less" )]
public class TSPackage : TypeScriptPackage
{
}
