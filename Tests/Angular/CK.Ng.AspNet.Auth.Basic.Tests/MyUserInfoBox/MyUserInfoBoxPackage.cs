using CK.Core;
using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.AspNet.Auth.Basic.Tests.MyUserInfoBox;


[TypeScriptPackage]
[Requires<INgUserInfoBoxComponent>]
public sealed class MyUserInfoBoxPackage : TypeScriptPackage
{
}
