using System;

namespace CK.TypeScript;


public interface ITypeScriptTypeDecorationAttribute
{
    /// <summary>
    /// Gets or sets an optional sub folder that will contain the TypeScript generated code.
    /// There must be no leading '/' or '\': the path is relative to the TypeScript output path of each BinPath.
    /// <para>
    /// This folder cannot be set to a non null path if <see cref="SameFolderAs"/> or <see cref="SameFileAs"/> is set to a non null type.
    /// </para>
    /// <para>
    /// When let to null, the folder will be derived from the type's namespace (unless <see cref="SameFolderAs"/> is set).
    /// When <see cref="string.Empty"/>, the file will be in the root folder of the TypeScript output path.
    /// </para>
    /// </summary>
    string? Folder { get; set; }

    /// <summary>
    /// Gets or sets the file name that will contain the TypeScript generated code.
    /// When not null, this must be a valid file name that ends with a '.ts' extension.
    /// <para>
    /// This must be null if <see cref="SameFileAs"/> is not null.
    /// </para>
    /// </summary>
    string? FileName { get; set; }

    /// <summary>
    /// Gets or sets another type which defines the final <see cref="Folder"/> and <see cref="FileName"/>.
    /// Both Folder and FileName MUST be null otherwise an <see cref="InvalidOperationException"/> is raised (conversely, Folder and FileName can
    /// be set to non null values only if this SameFileAs is null).
    /// </summary>
    Type? SameFileAs { get; set; }

    /// <summary>
    /// Gets or sets another type which defines the <see cref="Folder"/>.
    /// Folder MUST be null and <see cref="SameFileAs"/> must be null or be the same as the new value otherwise
    /// an <see cref="InvalidOperationException"/> is raised.
    /// <para>
    /// This defaults to <see cref="SameFileAs"/>.
    /// </para>
    /// </summary>
    Type? SameFolderAs { get; set; }

    /// <summary>
    /// Gets or sets the TypeScript type name to use for this type.
    /// </summary>
    string? TypeName { get; set; }
}
