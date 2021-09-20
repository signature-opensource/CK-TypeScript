using System;
using System.Collections.Generic;
using System.Text;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Marker interface that enables discovery of <see cref="TypeScriptAttributeImpl"/>, global <see cref="CK.Setup.ITSCodeGenerator"/>
    /// and <see cref="CK.Setup.ITSCodeGeneratorType"/> in a single pass.
    /// </summary>
    /// <remarks>
    /// This publicly exposed since base interfaces of public interfaces are required to be public, but this is
    /// for internal use.
    /// </remarks>
    public interface ITSCodeGeneratorAutoDiscovery
    {
    }
}
