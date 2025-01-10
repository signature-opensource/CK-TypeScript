using System.Collections.Generic;

namespace CK.TypeScript.Transform;

/// <summary>
/// Read only description of an "import ..." statement.
/// </summary>
public interface IImportLine
{
    /// <summary>
    /// This is an "import '...';". No symbols are imported.
    /// </summary>
    bool SideEffectOnly { get; }

    /// <summary>
    /// This is an "import type ... ".
    /// </summary>
    bool TypeOnly { get; }

    /// <summary>
    /// Gets the module namespace. "import * as API from '...'".
    /// There is necessarily nothing after (no named imports nor default imports).
    /// <para>
    /// Use of default export/import is discouraged. They must be used only for other external code that requires them.
    /// </para>
    /// </summary>
    string? Namespace { get; }

    /// <summary>
    /// Gets the default import. This is necessarily the first identifier: "import API from '...'".
    /// There can be named imports after.
    /// <para>
    /// Use of default export/import is discouraged. They must be used only for other external code that requires them.
    /// </para>
    /// </summary>
    string? DefaultImport { get; }

    /// <summary>
    /// Gets the named imports if any.
    /// </summary>
    IReadOnlyList<ImportLine.NamedImport> NamedImports { get; }

    /// <summary>
    /// Gets the import path (without the quotes).
    /// </summary>
    string ImportPath { get; set; }
}
