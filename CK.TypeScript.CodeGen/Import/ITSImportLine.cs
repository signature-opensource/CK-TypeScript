using System.Collections.Generic;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// TypeScript imports are grouped by their source module: a source of import is either a <see cref="LibraryImport"/> or
/// a <see cref="TypeScriptFileBase"/> or the "@local/ck-gen" alias.
/// </summary>
public interface ITSImportLine
{
    /// <summary>
    /// Gets whether this line of imports is from the '@local/ck-gen' module.
    /// </summary>
    bool FromLocalCkGen { get; }

    /// <summary>
    /// Gets the non null library if this line of imports is from a library.
    /// </summary>
    LibraryImport? FromLibrary { get; }

    /// <summary>
    /// Gets the non null source file if this line of imports is a generated code file.
    /// </summary>
    TypeScriptFileBase? FromTypeScriptFile { get; }

    /// <summary>
    /// Gets the name of the symbol that imports the default export of <see cref="FromLibrary"/>
    /// or <see cref="FromTypeScriptFile"/> ("@local/ck-gen" is a barrel has no default export).
    /// <para>
    /// This can be set by adding a "default symbolName" to this line.
    /// </para>
    /// </summary>
    string? DefaultImportSymbol { get; }

    /// <summary>
    /// Gets the list of imported names.
    /// </summary>
    IReadOnlyList<TSImportedName> ImportedNames { get; }

    /// <summary>
    /// Imports one or more type or variable names. Multiple symbols must be separated by commas.
    /// <para>
    /// Symbols can be aliased. Note that "UserInfo as Info, User, UserInfo" is valid: the "UserInfo"
    /// is available as "Info" but also as "UserInfo".
    /// </para>
    /// <para>
    /// To import the default export from the module, prefix the default name with "default " (order doesn't matter):
    /// "AxiosInstance, default axios" is rewritten to "import axios, { AxiosInstance } from ..."
    /// </para>
    /// <para>
    /// The default exported name must always be the same: if it has alredy been defined to a different name,
    /// an <see cref="System.InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="symbolNames">Comma separated of type or variable names to import.</param>
    void Add( string symbolNames );
}
