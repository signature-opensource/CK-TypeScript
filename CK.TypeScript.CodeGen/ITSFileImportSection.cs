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
        /// Ensures that an import of the type name from the corresponding file exists.
        /// </summary>
        /// <param name="typeName">The imported type name.</param>
        /// <param name="file">The referenced file.</param>
        void EnsureImport( string typeName, TypeScriptFile file );

        /// <summary>
        /// Gets the current import code section.
        /// </summary>
        /// <returns>The import section. Can be empty.</returns>
        string ToString();
    }
}
