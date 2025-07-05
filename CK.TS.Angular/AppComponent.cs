namespace CK.TS.Angular;

/// <summary>
/// Models the Angular application component.
/// <para>
/// This component is not in the /ck-gen folder: it exists by design and has .html, .less and .ts files
/// that are out of our scope.
/// </para>
/// </summary>
[NgComponent( NgComponentAttribute.BaseActualAttributeTypeAssemblyQualifiedName,
              HasRoutes = true,
              TypeScriptFolder = "../src/app" )]
public sealed class AppComponent : NgComponent
{
}
