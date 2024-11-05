using CK.StObj.TypeScript;

namespace CK.Ng.AspNet.Auth;

[TypeScriptPackage]
[TypeScriptImportLibrary( "@angular/core", "^18.2.8", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@angular/common", "^18.2.8", DependencyKind.Dependency, ForceUse = true )]
public class TSPackage : TypeScriptPackage
{
    void StObjConstruct( CK.AspNet.Auth.TSPackage aspNetAuthPackage ) { }
}
