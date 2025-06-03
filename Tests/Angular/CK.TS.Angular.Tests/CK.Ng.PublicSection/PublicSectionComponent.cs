using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.PublicSection;

[NgComponent]
[Requires<Zorro.TSPackage>]
//
// You can also define the Package (or Group) children like this instead of
// using [Package<...>] or [Group<...>].
//
[Children<PublicTopbarComponent, PublicFooterComponent>]
public sealed class PublicSectionComponent : NgComponent, INgNamedComponent // Testing the CK/Angular/NamedComponentResolver.ts
{
}
