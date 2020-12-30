using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// The body section of a <see cref="TypeScriptFile"/>.
    /// </summary>
    public interface ITSFileBodySection : ITSCodePart
    {
        /// <summary>
        /// Gets the file of this body section.
        /// </summary>
        TypeScriptFile File { get; }

        /// <summary>
        /// Gets the current body code section.
        /// </summary>
        /// <returns>The body section. Can be empty.</returns>
        string ToString();
    }
}
