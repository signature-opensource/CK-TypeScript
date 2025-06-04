using CK.TypeScript;
using CK.TS.Angular;

namespace CK.Ng.Axios;

/// <summary>
/// Imports axios library and provides the Angular AXIOS injection token
/// for the default AxiosInstance to use.
/// </summary>
[TypeScriptPackage( TypeScriptFolder = "CK/Ng" )]
[TypeScriptImportLibrary( "axios", "^1.7.7", DependencyKind.PeerDependency, ForceUse = true )]
[TypeScriptFile( "AXIOSToken.ts", "AXIOS" )]
[NgProviderImport( "AXIOS" )]
[NgProviderImport( "default axios, AxiosInstance", LibraryName = "axios" )]
[NgProvider( "{ provide: AXIOS, useValue: axios.create() }" )]
public class TSPackage : TypeScriptPackage
{
}
