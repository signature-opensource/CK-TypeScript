using System;
using System.Collections.Generic;
using System.IO;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// A minimal TypeScript file holds exported type declaration but has no parts
/// or other means to define the code itself. Available implementations are:
/// <list type="bullet">
///     <item>The regular <see cref="TypeScriptFile"/> that is used to fully generate code.</item>
///     <item>The resource based <see cref="ResourceTypeScriptFile"/>.</item>
/// </list>
/// </summary>
public interface IMinimalTypeScriptFile
{
    /// <inheritdoc cref="BaseFile.Root" />
    TypeScriptFolder Folder { get; }

    /// <inheritdoc cref="TypeScriptFolder.Root" />
    TypeScriptRoot Root { get; }

    /// <inheritdoc cref="BaseFile.Name" />
    string Name { get; }

    /// <inheritdoc cref="BaseFile.Extension" />
    ReadOnlySpan<char> Extension { get; }

    /// <summary>
    /// Gets the all the TypeScript types that are defined in this <see cref="File"/>.
    /// </summary>
    IEnumerable<ITSDeclaredFileType> AllTypes { get; }

    /// <summary>
    /// Declares only a <see cref="ITSDeclaredFileType"/> in this file: the <paramref name="typeName"/> is implemented
    /// in this file but not in a specific <see cref="ITSCodePart"/>.
    /// <para>
    /// The <paramref name="typeName"/> must not already exist in the <see cref="TSTypeManager"/>.
    /// </para>
    /// </summary>
    /// <param name="typeName">The TypeScript type name.</param>
    /// <param name="additionalImports">The required imports. Null when using this type requires only this file.</param>
    /// <param name="defaultValueSource">The type default value if any.</param>
    /// <returns>A TS type in this file (but with no associated <see cref="ITSCodePart"/>).</returns>
    ITSDeclaredFileType DeclareType( string typeName,
                                     Action<ITSFileImportSection>? additionalImports = null,
                                     string? defaultValueSource = null );
}
