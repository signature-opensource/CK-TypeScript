using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.TypeScript.CodeGen;


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
    /// Imports one or more type or variable names from an external library.
    /// See <see cref="ITSImportLine.Add(string)"/>.
    /// </summary>
    /// <param name="library">The library from which symbols must be imported.</param>
    /// <param name="symbolNames">Comma separated of type or variable names to import.</param>
    void ImportFromLibrary( LibraryImport library, string symbolNames );

    /// <summary>
    /// Imports one or more type or variable names from a sub path in an external library.
    /// </summary>
    /// <param name="source">The library and the subordinate path. The path may start or not with a '/'.</param>
    /// <param name="symbolNames">Comma separated of type or variable names to import.</param>
    void ImportFromLibrary( (LibraryImport Library, string SubPath) source, string symbolNames );

    /// <summary>
    /// Imports one or more type or variable names from an external library.
    /// See <see cref="ITSImportLine.Add(string)"/>.
    /// </summary>
    /// <param name="file">The file from which symbols must be imported.</param>
    /// <param name="symbolNames">Comma separated of type or variable names to import.</param>
    void ImportFromFile( TypeScriptFileBase file, string symbolNames );

    /// <summary>
    /// Imports one or more type or variable names from generated code files.
    /// See <see cref="ITSImportLine.Add(string)"/>.
    /// </summary>
    /// <param name="symbolNames">Comma separated of type or variable names to import.</param>
    void ImportFromLocalCKGen( string symbolNames );

    /// <summary>
    /// Imports a <see cref="ITSType"/> in this file.
    /// </summary>
    /// <param name="tsType">The type to import.</param>
    /// <returns>This section to enable fluent syntax.</returns>
    ITSFileImportSection Import( ITSType tsType );

    /// <summary>
    /// Ensures that an import of one or more types exists in this file.
    /// <see cref="TSTypeManager.ResolveTSType(IActivityMonitor, object)"/> is called for each type.
    /// </summary>
    /// <param name="monitor">Required monitor since ResolveTSType is called.</param>
    /// <param name="type">The type to import.</param>
    /// <param name="types">Optional types to import.</param>
    /// <returns>This section to enable fluent syntax.</returns>
    ITSFileImportSection EnsureImport( IActivityMonitor monitor, Type type );

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
