using CK.StObj.TypeScript;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[TypeScriptPackage]
public class TSPackage : TypeScriptPackage
{
    void StObjConstruct( CK.AspNet.Auth.TSPackage aspNetAuth, CK.Ng.Axios.TSPackage axios ) { }
}

[NgProvider( "{ provide: AuthService, deps:[AXIOS], useFactory: (a : AxiosInstance) => new AuthService( a ) }" )]
[NgProvider( "{ provideNgAuthSupport()", "#Support" )]
[NgProviderImport( "@local/ck-gen", "AXIOS" )]
[NgProviderImport( "axios", "default axios", "AxiosInstance" )]
public class NgAuthServiceProvider : NgProvider
{
}
