using CK.Core;
using CK.TS.Angular;

namespace CK.Ng.PublicSection;

[NgComponent]
[Package<PublicSectionComponent>]
// This makes the ActionBarComponent no more optional: it appears in ck-gen.
//
// PublicSectionComponent OptionalRequires GenericFormComponent and have a transformer for it but
// since no one actually requires it, the GenericFormComponent is still optional: it doesn't appear in
// the final ck-gen.
//
[Requires<Ng.Zorro.ActionBarComponent>]
public sealed class PublicTopbarComponent : NgComponent
{
}

