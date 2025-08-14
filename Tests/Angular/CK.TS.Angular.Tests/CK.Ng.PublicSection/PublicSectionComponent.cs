using CK.Core;
using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.PublicSection;

[NgComponent]
[Requires<Zorro.ZorroPackage>]
//
// You can also define the Package (or Group) children like this instead of
// using [Package<...>] or [Group<...>].
//
[Children<PublicTopbarComponent, PublicFooterComponent>]

[TypeScriptFile( "SomeFolder/some-file.ts", "SomeSymbol" )]
#pragma warning disable CS0618 // Type or member is obsolete
[TypeScriptFile( "SomeFolder/some-other-file.ts", "SomeOtherSymbol", TargetFolder = "CK/Ng/PublicSection/public-section" )]
#pragma warning restore CS0618 // Type or member is obsolete

//
// Only an Optional requires.
// No one actually requires it, the GenericFormComponent is still optional: it doesn't appear in
// the final ck-gen.
// The transformer "generic-form.t" doesn't break the run because the still optional package is found
// and it is simply ignored.
//
[OptionalRequires<CK.Ng.Zorro.GenericFormComponent>]
public sealed class PublicSectionComponent : NgComponent, INgNamedComponent // Testing the CK/Angular/NamedComponentResolver.ts
{
}
