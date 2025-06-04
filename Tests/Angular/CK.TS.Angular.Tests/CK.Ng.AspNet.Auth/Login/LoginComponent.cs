using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgRoutedComponent<INgPublicPageComponent>( HasRoutes = true )]
[Package<SomeAuthPackage>]
public sealed class LoginComponent : NgRoutedComponent, INgNamedComponent // Testing the CK/Angular/NamedComponentResolver.ts
{
}

