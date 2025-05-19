using CK.Core;
using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.PublicSection;

[TypeScriptPackage]
[Requires<Zorro.TSPackage>]
//
// You can also define the Package (or Group) children like this instead of
// using [Package<...>] or [Group<...>].
//
[Children<PublicTopbarComponent, PublicFooterComponent>]
public sealed class PublicSectionPackage : TypeScriptPackage
{
}
