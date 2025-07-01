using System;
using System.Collections.Generic;
namespace CK.Core;

/// <summary>
/// Decorates an opt-in type: it must be explicitly registered (see <see cref="RegistrationMode.Explicit"/>)
/// or required by another registered component, otherwise it is ignored.
/// <para>
/// Engines that handles such optional types should ensure that this is applied to "implementation": there is no
/// such thing as an "optional abstraction", an abstraction is always "available" unless it has no registered "implementation".
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class|AttributeTargets.Struct|AttributeTargets.Interface, AllowMultiple = false, Inherited = false )]
public sealed class OptionalTypeAttribute : Attribute
{
}
