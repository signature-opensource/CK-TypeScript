using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// A TypeScript file resides in a definitive <see cref="TypeScriptFolder"/>
    /// and exposes a <see cref="Imports"/> and a <see cref="Body"/> sections.
    /// <para>
    /// This is the base class and non generic version of <see cref="TypeScriptFile{TRoot}"/>.
    /// </para>
    /// </summary>
    public class TypeScriptFile
    {
        internal TypeScriptFile? _next;

        internal TypeScriptFile( TypeScriptFolder folder, string name )
        {
            Folder = folder;
            Name = name;
            _next = folder._firstFile;
            folder._firstFile = this;
            Imports = new FileImportCodePart( this );
            Body = new FileBodyCodePart( this );
        }

        /// <summary>
        /// Gets the folder of this file.
        /// </summary>
        public TypeScriptFolder Folder { get; }

        /// <inheritdoc cref="TypeScriptFolder.Root" />
        public TypeScriptRoot Root => Folder.Root;

        /// <summary>
        /// Gets this file name.
        /// It necessarily ends with '.ts'.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the import section of this file.
        /// </summary>
        public ITSFileImportSection Imports { get; }

        /// <summary>
        /// Gets the code section of this file.
        /// </summary>
        public ITSFileBodySection Body { get; }

        /// <summary>
        /// Creates a part that is bound to this file but whose content
        /// is not in this <see cref="Body"/>.
        /// </summary>
        /// <returns></returns>
        public ITSCodePart CreateDetachedPart() => new RawCodePart( this, String.Empty );

        /// <summary>
        /// Saves this file into a folder on the file system.
        /// The <see cref="Body"/> can be null (only <see cref="Imports"/> if any will be generated).
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="outputPath">Target directory.</param>
        public void Save( IActivityMonitor monitor, NormalizedPath outputPath )
        {
            monitor.Trace( $"Saving '{Name}'." );
            var imports = Imports.ToString();
            if( imports.Length > 0 ) imports += Environment.NewLine;
            var all = imports + Body.ToString();
            File.WriteAllText( outputPath.AppendPart( Name ), all );
        }

        /// <summary>
        /// Overridden to return this file name.
        /// </summary>
        /// <returns>The <see cref="Name"/>.</returns>
        public override string ToString() => Name;
    }
}
