using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgRoutedComponent<AppComponent>( HasRoutes = true, Route = "auth" )]
[Package<AspNetAuthPackage>]
public sealed class AuthenticationPageComponent : NgRoutedComponent
{
}
