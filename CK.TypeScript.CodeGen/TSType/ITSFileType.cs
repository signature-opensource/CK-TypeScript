namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Defines a type that is implemented in a <see cref="File"/> by a <see cref="TypePart"/>.
    /// </summary>
    public interface ITSFileType : ITSDeclaredFileType
    {
        /// <summary>
        /// Gets the code part. 
        /// </summary>
        ITSCodePart TypePart { get; }
    }

}

