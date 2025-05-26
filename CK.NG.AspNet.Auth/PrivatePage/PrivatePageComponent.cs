using CK.Core;
using CK.TS.Angular;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Ng.AspNet.Auth;

[NgComponent( HasRoutes = true )]
[Package<TSPackage>]
public sealed class PrivatePageComponent : NgComponent, INgPrivatePageComponent
{
}
