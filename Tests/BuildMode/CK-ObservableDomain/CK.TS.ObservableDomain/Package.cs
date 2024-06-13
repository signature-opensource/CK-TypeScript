using CK.StObj.TypeScript;

namespace CK.ObservableDomain
{
    [TypeScriptPackage]
    [ImportTypeScriptLibrary("rxjs", "7.5.6", DependencyKind.Dependency)]
    [TypeScriptFile( "IObservableDomainLeagueDriver.ts", "IObservableDomainLeagueDriver" )]
    [TypeScriptFile( "ObservableDomain.ts", "ObservableDomain", "WatchEvent" )]
    [TypeScriptFile( "ObservableDomainClient.ts", "ObservableDomainClient", "ObservableDomainClientConnectionState" )]
    public class Package : TypeScriptPackage
    {
        void StObjConstruct( CK.JsonGraphSerializer.Package graphSerializer ) { }
    }
}
