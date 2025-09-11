namespace CK.TypeScript.CodeGen;

/// <summary>
/// Defines a type that is implemented in a <see cref="File"/>.
/// </summary>
public interface ITSDeclaredFileType : ITSType
{
    /// <summary>
    /// Gets the file that declares this type.
    /// </summary>
    TypeScriptFileBase File { get; }

    /// <summary>
    /// Gets the <see cref="File"/>'s folder path concatenated with the File's
    /// name without the ".ts" extension. This is computed once and cached.
    /// <para>
    /// This doesn't start with the "@local/ck-gen" prefix.
    /// </para>
    /// </summary>
    string ImportPath { get; }
}

