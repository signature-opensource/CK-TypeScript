using CK.StObj.TypeScript;

namespace CK.AspNet.Auth;

/// <summary>
/// Client authentication package.
/// </summary>
[TypeScriptPackage]
[TypeScriptImportLibrary( "axios", "^1.7.2", DependencyKind.PeerDependency, ForceUse = true )]
public class TSPackage : TypeScriptPackage
{
}
