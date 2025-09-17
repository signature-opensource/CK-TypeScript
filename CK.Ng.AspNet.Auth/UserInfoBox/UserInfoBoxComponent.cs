using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth;

[NgComponent]
[Package<AspNetAuthPackage>]
public sealed class UserInfoBoxComponent : NgComponent, INgUserInfoBoxComponent
{
}
