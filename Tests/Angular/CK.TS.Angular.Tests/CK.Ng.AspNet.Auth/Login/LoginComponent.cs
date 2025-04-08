using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgRoutedComponent( typeof( AppComponent ), HasRoutes = true )]
[Package<SomeAuthPackage>]
public sealed class LoginComponent : NgRoutedComponent
{
}

