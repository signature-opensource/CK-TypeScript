namespace CK.StObj.TypeScript
{
    public interface ITypeScriptTypeConfiguration
    {
        /// <summary>
        /// Gets or sets an optional sub folder that will contain the TypeScript generated code.
        /// There must be no leading '/' or '\': the path is relative to the TypeScript output path of each <see cref="BinPathConfiguration"/>.
        /// <para>
        /// When let to null, the folder will be derived from the type's namespace.
        /// When <see cref="string.Empty"/>, the file will be in the root folder of the TypeScript output path.
        /// </para>
        /// </summary>
        string? FileName { get; }
        string? Folder { get; }
        string? TypeName { get; }
    }
}
