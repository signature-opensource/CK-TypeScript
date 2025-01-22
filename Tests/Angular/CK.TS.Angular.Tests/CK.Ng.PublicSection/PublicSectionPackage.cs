using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.PublicSection;


[TypeScriptPackage]
public sealed partial class PublicSectionPackage : TypeScriptPackage
{
}

// Test the LibraryName + SubPath.
[TypeScriptImportLibrary( "ng-zorro-antd", "^19", DependencyKind.Dependency, ForceUse = true )]
[NgProviderImport( "NZ_I18N", LibraryName = "ng-zorro-antd/i18n" )]
[NgProviderImport( "fr_FR", LibraryName = "ng-zorro-antd/i18n" )]
[NgProvider( "{ provide: NZ_I18N, useValue: fr_FR }" )]
public sealed partial class PublicSectionPackage : TypeScriptPackage
{
}
