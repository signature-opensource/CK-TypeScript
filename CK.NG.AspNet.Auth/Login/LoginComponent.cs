using CK.Core;
using CK.Ng.AspNet.Auth.AuthenticationPage;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth.Login;

[NgRoutedComponent<AuthenticationPageComponent>( RegistrationMode = RouteRegistrationMode.Lazy )]
[Package<TSPackage>]
public sealed class LoginComponent : NgRoutedComponent
{
}
