using CK.TypeScript;
using CK.TS.Angular;
using CK.Core;

namespace CK.Ng.AspNet.Auth;

/// <summary>
/// Provides the default AuthService configured to use the default AXIOS
/// instance from <see cref="Axios.AxiosPackage"/>.
/// <para>
/// An interceptor is injected into the AxiosInstance that handles the bearer token
/// for the current authentication information automatically and the initial
/// authentication information is automatically refreshed from the backend.
/// </para>
/// </summary>
[TypeScriptPackage]
[Requires<CK.AspNet.Auth.AspNetAuthPackage, CK.Ng.Axios.AxiosPackage, CK.Ng.Zorro.ZorroPackage>]
[NgProviderImport( "inject", From = "@angular/core" )]
[NgProviderImport( "AXIOS, AUTH_SERVICE_CONFIGURATION, AuthService" )]
[NgProviderImport( "provideNgAuthSupport", From = "@local/ck-gen/CK/Ng/AspNet/Auth/auth-service-support" )]
[NgProvider( "{ provide: AuthService, useFactory: () => new AuthService( inject( AXIOS ), (inject( AUTH_SERVICE_CONFIGURATION, { optional: true } ) ?? undefined) ) }" )]
[NgProvider( "provideNgAuthSupport()", "#Support" )]
[TypeScriptFile( "AUTH_SERVICE_CONFIGURATIONToken.ts", "AUTH_SERVICE_CONFIGURATION" )]
[TypeScriptFile( "NgAuthService.ts", "NgAuthService" )]
public class AspNetAuthPackage : TypeScriptPackage
{
}
