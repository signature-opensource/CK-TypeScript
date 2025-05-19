using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.Zorro;

[TypeScriptPackage]
[TypeScriptImportLibrary( "ng-zorro-antd", "^19", DependencyKind.Dependency, ForceUse = true )]
[AppStyleImport( "ng-zorro-antd/ng-zorro-antd.less" )]
public class TSPackage : TypeScriptPackage
{
}
