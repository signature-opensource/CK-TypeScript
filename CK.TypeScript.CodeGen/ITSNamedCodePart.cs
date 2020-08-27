using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Extends <see cref="ITSCodePart"/> with an identifier name.
    /// </summary>
    public interface ITSNamedCodePart : ITSCodePart
    {
        /// <summary>
        /// Gets the part name that identifies this part in its parent.
        /// </summary>
        string Name { get; }
    }
}
