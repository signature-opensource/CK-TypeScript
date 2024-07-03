using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Captures an Assembly and a resource name that is the origin
    /// of a <see cref="TypeScriptFile.Origin"/>.
    /// </summary>
    /// <param name="Assembly">The assembly from which the resource has been loaded.</param>
    /// <param name="ResourceName">The resource name in the assembly.</param>
    public record class OriginResource( Assembly Assembly, string ResourceName );

}
