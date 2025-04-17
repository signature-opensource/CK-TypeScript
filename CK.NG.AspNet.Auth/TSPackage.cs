using CK.TypeScript;
using CK.TS.Angular;
using CK.Core;

namespace CK.Ng.AspNet.Auth;

/// <summary>
/// Provides the default AuthService configured to use the default AXIOS
/// instance from <see cref="Axios.TSPackage"/>.
/// <para>
/// An interceptor is injected into the AxiosInstance that handles the bearer token
/// for the current authentication information automatically and the initial
/// authentication information is automatically refreshed from the backend.
/// </para>
/// </summary>
[TypeScriptPackage]
[Requires<CK.AspNet.Auth.TSPackage, CK.Ng.Axios.TSPackage>]
[NgProviderImport( "AXIOS, provideNgAuthSupport, AuthService" )]
[NgProviderImport( "AxiosInstance", LibraryName = "axios" )]
[NgProviderImport( "inject", LibraryName = "@angular/core" )]
[NgProvider( "{ provide: AuthService, useFactory: () => new AuthService( inject( AXIOS ) ) }" )]
[NgProvider( "provideNgAuthSupport()", "#Support" )]
public class TSPackage : TypeScriptPackage
{
}
