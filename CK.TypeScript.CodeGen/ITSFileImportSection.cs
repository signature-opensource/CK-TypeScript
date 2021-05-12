using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// The import section of a <see cref="TypeScriptFile"/> is a <see cref="ITSCodeWriter"/>
    /// that generates a first part with imports declarations from <see cref="EnsureImport(string, TypeScriptFile)"/>.
    /// </summary>
    public interface ITSFileImportSection : ITSCodeWriter
    {
        /// <summary>
        /// Ensures that an import of the type name from the corresponding file exists.
        /// </summary>
        /// <param name="typeName">The imported type name.</param>
        /// <param name="file">The referenced file.</param>
        void EnsureImport( string typeName, TypeScriptFile file );

        /// <summary>
        /// Gets the number of different <see cref="EnsureImport(string, TypeScriptFile)"/>
        /// that have been done.
        /// </summary>
        int ImportCount { get; }

        /// <summary>
        /// Gets the current import code section.
        /// </summary>
        /// <returns>The import section. Can be empty.</returns>
        string ToString();
    }
}
