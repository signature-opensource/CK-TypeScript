namespace CK.TypeScript.CodeGen;

/// <summary>
/// Defines a type that is implemented in a <see cref="File"/>.
/// </summary>
public interface ITSDeclaredFileType : ITSType
{
    /// <summary>
    /// Gets the file that declares this type.
    /// </summary>
    IMinimalTypeScriptFile File { get; }
}

