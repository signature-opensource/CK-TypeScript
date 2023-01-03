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
        /// Gets the type script root.
        /// </summary>
        public TypeScriptGenerator Root => Folder.Generator;

        /// <summary>
        /// Creates a part that is bound to this file but whose content
        /// is not in this <see cref="Body"/>.
        /// </summary>
        /// <returns></returns>
        public ITSCodePart CreateDetachedPart() => new RawCodePart( this, String.Empty );

        /// <summary>
        /// Saves this file into one or more actual paths on the file system.
        /// The <see cref="Body"/> can be null (only <see cref="Imports"/> if any will be generated).
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="outputPaths">Any number of target directories.</param>
        /// <returns>True on success, false is an error occurred (the error has been logged).</returns>
        public void Save( IActivityMonitor monitor, IEnumerable<NormalizedPath> outputPaths )
        {
            monitor.Trace( $"Saving '{Name}'." );
            var imports = Imports.ToString();
            if( imports.Length > 0 ) imports += Environment.NewLine;
            var all = imports + Body.ToString();
            foreach( var p in outputPaths )
            {
                File.WriteAllText( p.AppendPart( Name ), all );
            }
        }

        /// <summary>
        /// Overridden to return the full path of this file.
        /// </summary>
        /// <returns>This full path.</returns>
        public override string ToString() => $"{Folder.FullPath.Path}/{Name}";
    }
}
