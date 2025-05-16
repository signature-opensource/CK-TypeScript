using CK.Core;
using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.PublicSection;

[TypeScriptPackage]
[Requires<Zorro.TSPackage>]
[NgAppStyleImport( "app-styles-by-PublicSection.less", AfterContent = true )]
public sealed class PublicSectionPackage : TypeScriptPackage
{
}
