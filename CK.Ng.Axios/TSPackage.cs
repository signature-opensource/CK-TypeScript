using CK.TypeScript;
using CK.TS.Angular;

namespace CK.Ng.Axios;

[TypeScriptPackage]
[TypeScriptImportLibrary( "axios", "^1.7.7", DependencyKind.PeerDependency, ForceUse = true )]

// How can we support this with the new resource centric approach?
// A Dictionary<string,NormalizedPath> ResourceMappings in the PackageDescriptor?
// Was about to introduce this complexity... But... For files that are handled by a
// ResourceSpaceFolderHandler, this simply doesn't make sense! (Imagine a "ts-locales/de.jsonc"
// that is mapped to "Some/Namespace".)
// This has to be handled/controlled in some way.
//
// Here, if we don't support this, the file will be in CK/Ng/Axios instead of CK/Ng... Not a big deal.
// Maybe it's a good thing to NOT introduce mappings...
// Using the namespace as much as possible should ease maintenance...
// 

[TypeScriptFile( "AXIOSToken.ts", "AXIOS", TargetFolder ="CK/Ng" )]

[NgProviderImport( "AXIOS" )]
[NgProviderImport( "default axios, AxiosInstance", LibraryName = "axios" )]
[NgProvider( "{ provide: AXIOS, useValue: axios.create() }" )]
public class TSPackage : TypeScriptPackage
{
}
