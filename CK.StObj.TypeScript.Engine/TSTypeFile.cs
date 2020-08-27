using CK.Setup;
using CK.Text;
using CK.TypeScript.CodeGen;

namespace CK.StObj.TypeScript.Engine
{
    public class TSTypeFile
    {
        TypeScriptFile? _file;

        internal TSTypeFile( TypeScriptGenerator g, NormalizedPath folder, string fileName, string typeName )
        {
            Generator = g;
            Folder = folder;
            FileName = fileName;
            FullFilePath = folder.AppendPart( fileName );
            TypeName = typeName;
        }

        /// <summary>
        /// Gets the <see cref="TypeScriptGenerator"/>.
        /// </summary>
        public TypeScriptGenerator Generator { get; }

        /// <summary>
        /// Gets the folder that will contain the TypeScript generated code.
        /// </summary>
        public NormalizedPath Folder { get; }

        /// <summary>
        /// Gets the file name.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the <see cref="Folder"/>/<see cref="FileName"/> full path.
        /// </summary>
        public NormalizedPath FullFilePath { get; }

        /// <summary>
        /// Gets the TypeScript type name to use.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Gets the associated file if <see cref="EnsureFile"/> has been called.
        /// </summary>
        public TypeScriptFile? File => _file;

        /// <summary>
        /// Ensures that the <see cref="File"/> has been created.
        /// </summary>
        /// <returns>The associated file.</returns>
        public TypeScriptFile EnsureFile() => _file ??= Generator.Context.Root.FindOrCreateFile( FullFilePath );
    }
}
