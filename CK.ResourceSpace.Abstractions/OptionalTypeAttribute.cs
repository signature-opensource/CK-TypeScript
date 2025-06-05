using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// Decorates an opt-in type: it must be explicitly registered or required by another
/// registered component, otherwise it is ignored.
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
public sealed class OptionalTypeAttribute : Attribute
{
}
