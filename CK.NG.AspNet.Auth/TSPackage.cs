using CK.TypeScript;
using CK.TS.Angular;
using CK.Core;

namespace CK.Ng.AspNet.Auth;

[TypeScriptPackage]
[Requires<CK.AspNet.Auth.TSPackage>]
[NgProviderImport( "AXIOS, provideNgAuthSupport, AuthService" )]
[NgProviderImport( "AxiosInstance", LibraryName = "axios" )]
[NgProvider( "{ provide: AuthService, deps:[AXIOS], useFactory: (a : AxiosInstance) => new AuthService( a ) }" )]
[NgProvider( "provideNgAuthSupport()", "#Support" )]
public class TSPackage : TypeScriptPackage
{
    void StObjConstruct( CK.AspNet.Auth.TSPackage aspNetAuth, CK.Ng.Axios.TSPackage axios ) { }
}
