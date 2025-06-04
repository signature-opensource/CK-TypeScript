using CK.Core;
using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.PublicSection;

[NgComponent]
[Requires<Zorro.TSPackage>]
//
// You can also define the Package (or Group) children like this instead of
// using [Package<...>] or [Group<...>].
//
[Children<PublicTopbarComponent, PublicFooterComponent>]

[TypeScriptFile( "SomeFolder/some-file.ts", "SomeSymbol" )]
[TypeScriptFile( "SomeFolder/some-other-file.ts", "SomeOtherSymbol", TargetFolder = "CK/Ng/PublicSection/public-section" )]
public sealed class PublicSectionComponent : NgComponent, INgNamedComponent // Testing the CK/Angular/NamedComponentResolver.ts
{
}
