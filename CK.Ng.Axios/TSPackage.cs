using CK.TypeScript;
using CK.TS.Angular;

namespace CK.Ng.Axios;

[TypeScriptPackage]
[TypeScriptImportLibrary( "axios", "^1.7.7", DependencyKind.PeerDependency, ForceUse = true )]

// This can now be fully supported thanks to the ResPackageDescriptor.RemoveCodeHandledResource.
// And it will.

[TypeScriptFile( "AXIOSToken.ts", "AXIOS", TargetFolder ="CK/Ng" )]

[NgProviderImport( "AXIOS" )]
[NgProviderImport( "default axios, AxiosInstance", LibraryName = "axios" )]
[NgProvider( "{ provide: AXIOS, useValue: axios.create() }" )]
public class TSPackage : TypeScriptPackage
{
}
