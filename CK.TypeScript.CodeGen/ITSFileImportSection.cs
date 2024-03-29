using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// The import section of a <see cref="TypeScriptFile"/> generates a first part with imports declarations.
    /// <para>
    /// This section doesn't expose the file to which it belongs and this is intended. Code generators must work with <see cref="TypeScriptFile"/>
    /// and use parts locally, keeping this relationship explicit.
    /// </para>
    /// </summary>
    public interface ITSFileImportSection
    {
        /// <summary>
        /// Ensures that an import of one or more type names from an external library exists.
        /// </summary>
        /// <param name="libraryImport">The library infos.</param>
        /// <param name="typeName">The first required type name to import.</param>
        /// <param name="typeNames">More types to import (optionals).</param>
        /// <returns>This section to enable fluent syntax.</returns>
        ITSFileImportSection EnsureImportFromLibrary( LibraryImport libraryImport, string typeName, params string[] typeNames );

        /// <summary>
        /// Ensures that an import of one or more already resolved types exists in this file.
        /// </summary>
        /// <param name="tsType">The type to import.</param>
        /// <param name="tsTypes">Optional types to import.</param>
        /// <returns>This section to enable fluent syntax.</returns>
        ITSFileImportSection EnsureImport( ITSType tsType, params ITSType[] tsTypes );

        /// <summary>
        /// Ensures that an import of one or more types exists in this file.
        /// <see cref="TSTypeManager.ResolveTSType(IActivityMonitor, object)"/> is called for each type.
        /// </summary>
        /// <param name="monitor">Required monitor since ResolveTSType is called.</param>
        /// <param name="type">The type to import.</param>
        /// <param name="types">Optional types to import.</param>
        /// <returns>This section to enable fluent syntax.</returns>
        ITSFileImportSection EnsureImport( IActivityMonitor monitor, Type type, params Type[] types );

        /// <summary>
        /// Ensures that an import of one or more type names from the corresponding <see cref="TypeScriptFile"/> exists.
        /// </summary>
        /// <param name="file">The referenced file.</param>
        /// <param name="typeName">The first required type name to import.</param>
        /// <param name="typeNames">More types to import (optionals).</param>
        /// <returns>This section to enable fluent syntax.</returns>
        ITSFileImportSection EnsureImport( TypeScriptFile file, string typeName, params string[] typeNames );

        /// <summary>
        /// Ensures that TypeScript files for types are imported.
        /// <see cref="TSTypeManager.ResolveTSType(IActivityMonitor, object)"/> is called for each type.
        /// </summary>
        /// <param name="monitor">Required monitor since ResolveTSType is called.</param>
        /// <param name="types">Types to import.</param>
        /// <returns>This section to enable fluent syntax.</returns>
        ITSFileImportSection EnsureImport( IActivityMonitor monitor, IEnumerable<Type> types );

        /// <summary>
        /// Gets the number of different <see cref="EnsureImport(TypeScriptFile, string, string[])"/>
        /// that have been done.
        /// </summary>
        int ImportCount { get; }

        /// <summary>
        /// Gets the currently imported library names.
        /// </summary>
        IEnumerable<string> ImportedLibraryNames { get; }

        /// <summary>
        /// Gets the current import code section.
        /// </summary>
        /// <returns>The import section. Can be empty.</returns>
        string ToString();
    }
}
