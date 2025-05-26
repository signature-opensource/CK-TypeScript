using CK.Core;
using CK.Ng.PublicPage;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgRoutedComponent<PublicPageComponent>( HasRoutes = true )]
[Package<SomeAuthPackage>]
public sealed class LoginComponent : NgRoutedComponent
{
}

