using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CK.TypeScript.CodeGen

{
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
            Body = new RawCodePart( String.Empty );
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
        public ITSCodePart Body { get; }

        public void Save( IActivityMonitor monitor, IReadOnlyList<NormalizedPath> outputPaths )
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
        /// Overridden to return this file name.
        /// </summary>
        /// <returns>The <see cref="Name"/>.</returns>
        public override string ToString() => Name;
    }
}
