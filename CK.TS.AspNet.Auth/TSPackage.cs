using CK.StObj.TypeScript;

namespace CK.AspNet.Auth;

[TypeScriptPackage]
[TypeScriptImportLibrary( "axios", "1.7.2", DependencyKind.PeerDependency, ForceUse = true )]
[TypeScriptResourceFiles]
public class TSPackage : TypeScriptPackage
{
}
