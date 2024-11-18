using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgRoutedComponent<SomeAuthPackage>( typeof( LoginComponent ), RegistrationMode = RouteRegistrationMode.Lazy )]
public sealed class PasswordLostComponent : NgRoutedComponent
{
}

