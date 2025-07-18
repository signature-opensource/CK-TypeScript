using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgRoutedComponent<AppComponent>( HasRoutes = true, Route = "" )]
[Package<TSPackage>]
public sealed class PrivatePageComponent : NgRoutedComponent, INgPrivatePageComponent
{
}
