using CK.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// TypeScript code generation context exposes a <see cref="Root"/> that contains as many <see cref="TypeScriptFolder"/>
    /// and <see cref="TypeScriptFile"/> as needed that can ultimately be <see cref="TypeScriptFolder.Save"/>d.
    /// </summary>
    public class TypeScriptCodeGenerationContext
    {
        readonly HashSet<NormalizedPath> _paths;
        readonly bool _pascalCase;

        /// <summary>
        /// Initializes a new <see cref="TypeScriptCodeGenerationContext"/>.
        /// </summary>
        /// <param name="outputPaths">Non empty set of output paths.</param>
        /// <param name="pascalCase">Whether PascalCase identifiers should be generated instead of camelCase.</param>
        public TypeScriptCodeGenerationContext( IEnumerable<NormalizedPath> outputPaths, bool pascalCase )
        {
            if( outputPaths == null || !outputPaths.Any() ) throw new ArgumentException( "Must not be null or empty.", nameof(outputPaths) );
            _paths = new HashSet<NormalizedPath>( outputPaths );
            _pascalCase = pascalCase;
            Root = new TypeScriptFolder( this );
        }

        /// <summary>
        /// Gets whether PascalCase identifiers should be generated instead of camelCase.
        /// </summary>
        public bool PascalCase { get; }

        /// <summary>
        /// Gets the output paths. Never empty.
        /// </summary>
        public IReadOnlyCollection<NormalizedPath> OutputPaths => _paths;

        /// <summary>
        /// Gets the root folder into which type script files must be generated.
        /// </summary>
        public TypeScriptFolder Root { get; }
    }
}
