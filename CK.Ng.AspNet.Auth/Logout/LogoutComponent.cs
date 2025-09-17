using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgRoutedComponent<AuthenticationPageComponent>( RegistrationMode = RouteRegistrationMode.Lazy )]
[Package<AspNetAuthPackage>]
public sealed class LogoutComponent : NgRoutedComponent
{
}
