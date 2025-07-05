using CK.Core;
using CK.TypeScript;

namespace CK.ObservableDomain;

[TypeScriptPackage]
[Requires<CK.JsonGraphSerializer.TSPackage>]
[TypeScriptImportLibrary( "rxjs", "^7.5.6", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptFile( "IObservableDomainLeagueDriver.ts", "IObservableDomainLeagueDriver" )]
[TypeScriptFile( "ObservableDomain.ts", "ObservableDomain", "WatchEvent" )]
[TypeScriptFile( "ObservableDomainClient.ts", "ObservableDomainClient", "ObservableDomainClientConnectionState" )]
public class TSPackage : TypeScriptPackage
{
    void StObjConstruct( CK.JsonGraphSerializer.TSPackage graphSerializer ) { }
}
