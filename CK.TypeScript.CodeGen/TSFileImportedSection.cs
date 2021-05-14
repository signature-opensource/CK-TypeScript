namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Provides <see cref="AddType(string)"/> to <see cref="ITSFileImportSection.EnsureImport(string, TypeScriptFile)"/>
    /// (fluent syntax).
    /// </summary>
    public readonly struct TSFileImportedSection : ITSFileImportSection
    {
        readonly TypeScriptFile _file;

        internal TSFileImportedSection( TypeScriptFile f, TypeScriptFile importedFile )
        {
            _file = f;
            ImportedFile = importedFile;
        }

        /// <summary>
        /// Ensures that a type is in the import list of this <see cref="ImportedFile"/>.
        /// </summary>
        /// <param name="typeName">The type name to import.</param>
        /// <returns>This file imported section to enable fluent syntax.</returns>
        public TSFileImportedSection AddType( string typeName ) => _file.Imports.EnsureImport( typeName, ImportedFile );

        /// <summary>
        /// Gets the imported file: <see cref="AddType(string)"/> can be used on it.
        /// </summary>
        public TypeScriptFile ImportedFile { get; }

        TypeScriptFile ITSCodeWriter.File => _file;

        /// <inheritdoc />
        public TSFileImportedSection EnsureImport( string typeName, TypeScriptFile file ) => _file.Imports.EnsureImport( typeName, _file );

        /// <inheritdoc />
        public override string ToString() => _file.Imports.ToString();

        int ITSFileImportSection.ImportCount => _file.Imports.ImportCount;

        void ITSCodeWriter.DoAdd( string? code ) => _file.Imports.DoAdd( code );
    }
}
