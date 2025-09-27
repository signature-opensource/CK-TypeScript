using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgRoutedComponent<AuthenticationPageComponent>( Route = "" )]
[Package<AspNetAuthPackage>]
public sealed class LoginComponent : NgRoutedComponent
{
}
