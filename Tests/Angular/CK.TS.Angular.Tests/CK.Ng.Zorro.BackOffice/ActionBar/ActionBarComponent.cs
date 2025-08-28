using CK.Core;
using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.Zorro;

[NgComponent( IsOptional = true )]
[Package<BackOfficePackage>]
[TypeScriptFile( "action-bar-model.ts", "ActionBarContent", "ActionBarAction" )]
public sealed class ActionBarComponent : NgComponent
{
}
