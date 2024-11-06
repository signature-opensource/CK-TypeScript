using CK.StObj.TypeScript;

namespace CK.Ng.Axios;

[TypeScriptPackage]
[TypeScriptImportLibrary( "axios", "^1.7.7", DependencyKind.PeerDependency, ForceUse = true )]
[TypeScriptFile( "AXIOSToken.ts", "AXIOS", TargetFolderName ="CK/Ng" )]
public class TSPackage : TypeScriptPackage
{
}
