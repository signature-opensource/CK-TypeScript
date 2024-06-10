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
        internal const string _hiddenFileName = ".hidden-file.ts";

        readonly string _name;
        readonly ITSFileBodySection _body;
        readonly TypeScriptFolder _folder;
        internal readonly FileImportCodePart _imports;
        internal TypeScriptFile? _next;
        OriginResource? _origin;

        internal TypeScriptFile( TypeScriptFolder folder, string name )
        {
            _folder = folder;
            _name = name;
            _imports = new FileImportCodePart( this );
            _body = new FileBodyCodePart( this );
            if( name != _hiddenFileName )
            {
                _next = folder._firstFile;
                folder._firstFile = this;
            }
        }

        /// <summary>
        /// Gets the folder of this file.
        /// </summary>
        public TypeScriptFolder Folder => _folder;

        /// <inheritdoc cref="TypeScriptFolder.Root" />
        public TypeScriptRoot Root => Folder.Root;

        /// <summary>
        /// Gets this file name.
        /// It necessarily ends with '.ts'.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets the import section of this file.
        /// </summary>
        public ITSFileImportSection Imports => _imports;

        /// <summary>
        /// Gets the code section of this file.
        /// </summary>
        public ITSFileBodySection Body => _body;

        /// <summary>
        /// Creates a part that is bound to this file but whose content
        /// is not in this <see cref="Body"/>.
        /// </summary>
        /// <returns>A detached part.</returns>
        public ITSCodePart CreateDetachedPart() => new RawCodePart( this, String.Empty );

        /// <summary>
        /// Saves this file into a folder on the file system.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="saver">The <see cref="TypeScriptFileSaveStrategy"/>.</param>
        public void Save( IActivityMonitor monitor, TypeScriptFileSaveStrategy saver )
        {
            if( _name != _hiddenFileName )
            {
                var filePath = saver._currentTarget.AppendPart( Name );
                saver.SaveFile( monitor, this, filePath );
            }
        }

        /// <summary>
        /// Gets or sets an optional <see cref="OriginResource"/> for this file.
        /// </summary>
        public OriginResource? Origin { get => _origin; set => _origin = value; }

        /// <summary>
        /// Overridden to return this file name.
        /// </summary>
        /// <returns>The <see cref="Name"/>.</returns>
        public override string ToString() => Name;
    }
}
