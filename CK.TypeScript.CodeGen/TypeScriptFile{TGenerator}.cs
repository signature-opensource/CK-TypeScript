using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Types adapter: this files exposes a typed <see cref="Folder"/>.
    /// </summary>
    /// <typeparam name="TGenerator">The actual type of the generator.</typeparam>
    public sealed class TypeScriptFile<TGenerator> : TypeScriptFile
        where TGenerator : TypeScriptGenerator
    {
        internal TypeScriptFile( TypeScriptFolder<TGenerator> root, string name )
            : base( root, name )
        {
        }

        /// <inheritdoc />
        public new TypeScriptFolder<TGenerator> Folder => Unsafe.As<TypeScriptFolder<TGenerator>>( base.Folder );

        /// <inheritdoc />
        public new TGenerator Root => Folder.Generator;

    }

}

