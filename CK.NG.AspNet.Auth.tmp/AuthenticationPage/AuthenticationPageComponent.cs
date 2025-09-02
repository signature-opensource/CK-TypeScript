using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgRoutedComponent<AppComponent>( HasRoutes = true, Route = "auth" )]
[Package<TSPackage>]
public sealed class AuthenticationPageComponent : NgRoutedComponent
{
}
