using CK.StObj.TypeScript;

namespace CK.AspNet.Auth
{
    [TypeScriptPackage]
    [ImportTypeScriptLibrary( "axios", "1.7.2", DependencyKind.Dependency, ForceUse = true )]
    [TypeScriptContentFiles( "Res" )]
    public class TSPackage : TypeScriptPackage
    {
    }
}
