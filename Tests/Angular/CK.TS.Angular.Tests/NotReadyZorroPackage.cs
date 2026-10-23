using CK.TypeScript;

namespace CK.TS.Angular.Tests;

/// <summary>
/// A CK package that declares the ng-zorro-antd major of Angular 20: it is not ready for the engine's Angular major.
/// </summary>
[TypeScriptPackage]
[TypeScriptImportLibrary( "ng-zorro-antd", "^20", DependencyKind.Dependency )]
public class NotReadyZorroPackage : TypeScriptPackage
{
}
