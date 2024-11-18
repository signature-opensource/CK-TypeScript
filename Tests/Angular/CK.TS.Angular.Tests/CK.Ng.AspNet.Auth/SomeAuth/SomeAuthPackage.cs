using CK.TypeScript;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[TypeScriptPackage]
[TypeScriptFile( "SomeAuthService.ts", "SomeAuthService" )]
[NgProviderImport( "SomeAuthService" )]
[NgProvider( "{provide: SomeAuthService, useValue: new SomeAuthService( 'Some explicit parameter' )}" )]
public sealed class SomeAuthPackage : TypeScriptPackage
{
}
