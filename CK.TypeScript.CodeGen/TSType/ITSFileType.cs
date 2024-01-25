namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Defines a type that is locally implemented in a <see cref="File"/>.
    /// </summary>
    public interface ITSFileType : ITSType
    {
        /// <summary>
        /// Gets the file that implements this type.
        /// </summary>
        TypeScriptFile File { get; }

        /// <summary>
        /// Gets the code part. 
        /// </summary>
        ITSCodePart TypePart { get; }
    }

}

