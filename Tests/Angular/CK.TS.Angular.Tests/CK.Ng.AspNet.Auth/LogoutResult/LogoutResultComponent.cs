using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgRoutedComponent<LogoutConfirmComponent>( RegistrationMode = RouteRegistrationMode.Lazy )]
[Package<SomeAuthPackage>]
public sealed class LogoutResultComponent : NgRoutedComponent
{
}

