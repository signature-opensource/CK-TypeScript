using CK.StObj.TypeScript;

namespace CK.Ng.AspNet.Auth;

[TypeScriptPackage]
[TypeScriptImportLibrary( "@angular/core", "^18.2.8", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@angular/common", "^18.2.8", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptResourceFiles] // Note: this should/will be removed, since we can assume that typescript packages come with resource files.
public class TSPackage : TypeScriptPackage
{
    void StObjConstruct( CK.AspNet.Auth.TSPackage aspNetAuthPackage ) { }
}
