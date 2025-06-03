using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth.Basic;

[NgRoutedComponent<AuthenticationPageComponent>( RegistrationMode = RouteRegistrationMode.Lazy )]
[Package<TSPackage>]
public sealed class PasswordLostComponent : NgRoutedComponent
{
}
