using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Types adapter: this files exposes a typed <see cref="Folder"/>.
    /// </summary>
    /// <typeparam name="TRoot">The actual type of the root.</typeparam>
    public sealed class TypeScriptFile<TRoot> : TypeScriptFile
        where TRoot : TypeScriptRoot
    {
        internal TypeScriptFile( TypeScriptFolder<TRoot> root, string name )
            : base( root, name )
        {
        }

        /// <inheritdoc />
        public new TypeScriptFolder<TRoot> Folder => (TypeScriptFolder<TRoot>)base.Folder;

    }

}

