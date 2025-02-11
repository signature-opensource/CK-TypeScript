using System;

namespace CK.TS.Angular;

/// <summary>
/// Models the Angular application component.
/// <para>
/// This component is not in the /ck-gen folder: it exists by design and has .html, .less and .ts files
/// that may be transformed.
/// </para>
/// <para>
/// This package contains all the <see cref="NgComponent"/> and <see cref="NgModule"/>.
/// </para>
/// </summary>
[NgComponent( disableResources: true,
              NgComponentAttribute.BaseActualAttributeTypeAssemblyQualifiedName,
              HasRoutes = true,
              TypeScriptFolder = "../src/app" )]
public sealed class AppComponent : NgComponent
{
}
