using CK.Core;
using CK.Ng.AspNet.Auth.AuthenticationPage;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth.Logout;

[NgRoutedComponent<AuthenticationPageComponent>( RegistrationMode = RouteRegistrationMode.Lazy )]
[Package<TSPackage>]
public sealed class LogoutComponent : NgRoutedComponent
{
}
