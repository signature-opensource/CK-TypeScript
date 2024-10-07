namespace CK.TypeScript.CodeGen;

/// <summary>
/// Most basic interface: a simple string fragment collector that belongs to a TypeScript <see cref="File"/>.
/// </summary>
public interface ITSCodeWriter
{
    /// <summary>
    /// Gets the file to which this writer belongs.
    /// </summary>
    TypeScriptFile File { get; }

    /// <summary>
    /// Gets whether this writer is empty.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Adds a raw string to this writer.
    /// </summary>
    /// <param name="code">Raw type script code. Can be null or empty.</param>
    void DoAdd( string? code );

}
