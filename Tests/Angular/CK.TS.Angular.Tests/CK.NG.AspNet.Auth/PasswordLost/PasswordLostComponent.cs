using CK.TS.Angular;

namespace CK.NG.AspNet.Auth;

[NgRoutedComponent<SomeAuthPackage>( typeof( LoginComponent ), RegistrationMode = RouteRegistrationMode.Lazy )]
public sealed class PasswordLostComponent : NgRoutedComponent
{
}

