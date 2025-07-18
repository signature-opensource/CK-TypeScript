using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.PublicPage;

[NgRoutedComponent<AppComponent>( HasRoutes = true, Route = "" )]
[OptionalRequires<INgPrivatePageComponent>]
public sealed class PublicPageComponent : NgRoutedComponent
{
}
