using CK.Core;

namespace CK.TS.Angular;

/// <summary>
/// Base class for routed components. A <see cref="NgRoutedComponentAttribute"/> must decorate the final type.
/// <para>
/// This kind of component targets a <see cref="NgComponent"/> with a true <see cref="NgComponentAttribute.HasRoutes"/> or
/// the <see cref="CKGenAppModule"/>.
/// </para>
/// </summary>
[CKTypeDefiner]
public abstract class NgRoutedComponent : NgComponent
{
}
