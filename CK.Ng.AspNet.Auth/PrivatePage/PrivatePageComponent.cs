using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgRoutedComponent<AppComponent>( HasRoutes = true, Route = "" )]
[Package<AspNetAuthPackage>]
public sealed class PrivatePageComponent : NgRoutedComponent, INgPrivatePageComponent
{
}
