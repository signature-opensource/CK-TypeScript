using CK.Auth;
using CK.TypeScript;

namespace CK.AspNet.Auth;

/// <summary>
/// Client authentication package.
/// </summary>
[TypeScriptPackage]
[TypeScriptImportLibrary( "axios", "^1.7.7", DependencyKind.PeerDependency )]
[RegisterTypeScriptType( typeof( AuthLevel ), Folder = "CK/AspNet/Auth" )]
[TypeScriptFile("AuthService.ts", "AuthService" )]
public class TSPackage : TypeScriptPackage
{
}
