using CK.Core;
using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.Zorro;

[NgComponent]
[Package<BackOfficePackage>]
[TypeScriptFile( "action-bar.model.ts", "ActionBarContent", "ActionBarAction" )]
public sealed class ActionBarComponent : NgComponent
{
}
