using CK.StObj.TypeScript;

namespace CK.TS.Angular;

/// <summary>
/// Models the Angular application component.
/// <para>
/// This component is not in the /ck-gen folder: it exists by design and has .html, .less and .ts files
/// that may be transformed.
/// </para>
/// </summary>
[NgComponent( HasRoutes = true, TypeScriptFolder = "../src/app" )]
public sealed class AppComponent : NgComponent, IRootTypeScriptPackage
{
}
