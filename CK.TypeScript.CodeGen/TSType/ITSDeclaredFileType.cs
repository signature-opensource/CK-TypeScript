namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Defines a type that is implemented in a <see cref="File"/>.
    /// </summary>
    public interface ITSDeclaredFileType : ITSType
    {
        /// <summary>
        /// Gets the file that implements this type.
        /// </summary>
        TypeScriptFile File { get; }
    }

}

