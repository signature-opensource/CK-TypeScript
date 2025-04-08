using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgRoutedComponent( typeof( LoginComponent ), RegistrationMode = RouteRegistrationMode.Lazy )]
[Package<SomeAuthPackage>]
public sealed class PasswordLostComponent : NgRoutedComponent
{
}

