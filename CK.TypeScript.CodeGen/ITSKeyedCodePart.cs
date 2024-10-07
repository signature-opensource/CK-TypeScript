namespace CK.TypeScript.CodeGen;

/// <summary>
/// Extends <see cref="ITSCodePart"/> with a key that identifies it and
/// an object tag that can be used freely.
/// </summary>
public interface ITSKeyedCodePart : ITSCodePart
{
    /// <summary>
    /// A standard string that can be used to identify an extension point in a
    /// constructor method when using <see cref="ITSCodePart.CreateKeyedPart(object, string, bool)"/>.
    /// <para>
    /// This is a convention that enables any TSType implemented in a file to expose its constructor body
    /// in its <see cref="ITSFileType.TypePart"/>.
    /// </para>
    /// </summary>
    public const string ConstructorBodyPart = "ConstructorBody";

    /// <summary>
    /// Gets the key that identifies this part in its parent.
    /// </summary>
    object Key { get; }
}
