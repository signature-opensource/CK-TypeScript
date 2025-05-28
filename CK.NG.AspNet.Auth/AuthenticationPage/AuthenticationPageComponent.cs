using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth.AuthenticationPage;

[NgRoutedComponent<AppComponent>( HasRoutes = true, Route = "auth" )]
[Package<TSPackage>]
public sealed class AuthenticationPageComponent : NgRoutedComponent, INgPublicPageComponent
{
}
