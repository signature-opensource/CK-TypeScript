using CK.Core;
using CK.TypeScript.CodeGen;
using System.Collections.Generic;
using System.Xml.Linq;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace CK.Setup
{
    /// <summary>
    /// Extends <see cref="TypeScriptRoot"/> in the context of code generation.
    /// </summary>
    public sealed class TypeScriptContextRoot : TypeScriptRoot
    {
        internal TypeScriptContextRoot( TypeScriptContext context,
                                        IReadOnlyCollection<(NormalizedPath Path, XElement Config)> outputPaths,
                                        TypeScriptAspectConfiguration config )
            : base( outputPaths, config.LibraryVersions, config.PascalCase, config.GenerateDocumentation )
        {
            Context = context;
        }

        /// <inheritdoc cref="TypeScriptRoot.Root" />
        public new TypeScriptFolder<TypeScriptContextRoot> Root => (TypeScriptFolder<TypeScriptContextRoot>)base.Root;

        /// <summary>
        /// Gets the <see cref="TypeScriptContext"/>.
        /// </summary>
        public TypeScriptContext Context { get; }
    }
}
