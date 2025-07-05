using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgRoutedComponent<AuthenticationPageComponent>( RegistrationMode = RouteRegistrationMode.Lazy )]
[Package<TSPackage>]
public sealed class LogoutComponent : NgRoutedComponent
{
}
