using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// The import section of a <see cref="TypeScriptFile"/>.
    /// </summary>
    public interface ITSFileImportSection : ITSCodeWriter
    {
        /// <summary>
        /// Gets the file of this import section.
        /// </summary>
        TypeScriptFile File { get; }

        /// <summary>
        /// Gets the current import code section.
        /// </summary>
        /// <returns>The import section. Can be empty.</returns>
        string ToString();
    }
}
