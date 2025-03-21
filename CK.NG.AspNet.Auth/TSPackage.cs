using CK.TypeScript;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[TypeScriptPackage]
[NgProviderImport( "AXIOS, provideNgAuthSupport, AuthService" )]
[NgProviderImport( "AxiosInstance", LibraryName = "axios" )]
[NgProviderImport( "inject", LibraryName = "@angular/core" )]
[NgProvider( "{ provide: AuthService, useFactory: () => new AuthService( inject( AXIOS ) ) }" )]
[NgProvider( "provideNgAuthSupport()", "#Support" )]
public class TSPackage : TypeScriptPackage
{
    void StObjConstruct( CK.AspNet.Auth.TSPackage aspNetAuth, CK.Ng.Axios.TSPackage axios ) { }
}
