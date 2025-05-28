using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgComponent( HasRoutes = true )]
[Package<TSPackage>]
public sealed class PrivatePageComponent : NgComponent, INgPrivatePageComponent
{
}
