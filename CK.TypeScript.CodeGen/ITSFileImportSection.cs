using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// The import section of a <see cref="TypeScriptFile"/> is a <see cref="ITSCodeWriter"/>
    /// that generates a first part with imports declarations from <see cref="EnsureImport(TypeScriptFile, string, string[])"/>.
    /// </summary>
    public interface ITSFileImportSection : ITSCodeWriter
    {
        /// <summary>
        /// Imported external library used by the generated code.
        /// </summary>
        IReadOnlyDictionary<string, LibraryImport> LibraryImports { get; }

        /// <summary>
        /// Ensures that an import of one or more type names from the corresponding <see cref="TypeScriptFile"/> exists.
        /// </summary>
        /// <param name="file">The referenced file.</param>
        /// <param name="typeName">The first required type name to import.</param>
        /// <param name="typeNames">More types to import (optionals).</param>
        /// <returns>This section to enable fluent syntax.</returns>
        ITSFileImportSection EnsureImport( TypeScriptFile file, string typeName, params string[] typeNames );

        /// <summary>
        /// Ensures that an import of one or more type names from an external library exists.
        /// </summary>
        /// <param name="libraryImport">The library infos.</param>
        /// <param name="typeName">The first required type name to import.</param>
        /// <param name="typeNames">More types to import  (optionals).</param>
        /// <returns>This section to enable fluent syntax.</returns>
        ITSFileImportSection EnsureImportFromLibrary( LibraryImport libraryImport, string typeName, params string[] typeNames );

        /// <summary>
        /// Ensures that an external library will be present in the project.
        /// </summary>
        /// <param name="libraryImport">The library infos.</param>
        /// <returns>This section to enable fluent syntax.</returns>
        ITSFileImportSection EnsureLibrary( LibraryImport libraryImport );

        /// <summary>
        /// Gets the number of different <see cref="EnsureImport(TypeScriptFile, string, string[])"/>
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
