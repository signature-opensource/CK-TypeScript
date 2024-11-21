namespace CK.TypeScript.CodeGen;

/// <summary>
/// Base class for textual files.
/// </summary>
public abstract class TextFileBase : BaseFile
{
    private protected TextFileBase( TypeScriptFolder folder, string name )
        : base( folder, name )
    {
    }

    /// <summary>
    /// Gets the file content as a string regardless of its internal
    /// representation.
    /// </summary>
    /// <returns>The full file content.</returns>
    public abstract string GetCurrentText();
}
