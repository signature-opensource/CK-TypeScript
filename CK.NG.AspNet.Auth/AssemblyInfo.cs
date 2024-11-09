//
// We need to exclude RootTypeScriptPackage because even if CK.TS.Angular excludes it, we depend
// on CK.TS.AspNet.Auth that includes it (it doesn't know anything about Angular
// and its AppComponent that is the IRootTypeScriptPackage for Angula applications).
//
[assembly: CK.Setup.ExcludeCKType( typeof( CK.StObj.TypeScript.RootTypeScriptPackage ) )]
