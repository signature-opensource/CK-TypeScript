using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Central TypeScript context with options and a <see cref="Root"/> that contains as many <see cref="TypeScriptFolder"/>
    /// and <see cref="TypeScriptFile"/> as needed that can ultimately be <see cref="TypeScriptFolder.Save"/>d.
    /// </summary>
    public class TypeScriptRoot
    {
        readonly HashSet<NormalizedPath> _paths;
        readonly bool _pascalCase;
        readonly bool _generateDocumentation;
        Dictionary<object, object?>? _memory;

        /// <summary>
        /// Initializes a new <see cref="TypeScriptRoot"/>.
        /// </summary>
        /// <param name="outputPaths">Set of output paths. May be empty.</param>
        /// <param name="pascalCase">Whether PascalCase identifiers should be generated instead of camelCase.</param>
        /// <param name="generateDocumentation">Whether documentation should be generated.</param>
        public TypeScriptRoot( IEnumerable<NormalizedPath> outputPaths, bool pascalCase, bool generateDocumentation )
        {
            if( outputPaths == null ) throw new ArgumentNullException( nameof( outputPaths ) );
            _paths = new HashSet<NormalizedPath>( outputPaths );
            _pascalCase = pascalCase;
            _generateDocumentation = generateDocumentation;
            Root = new TypeScriptFolder( this );
        }

        /// <summary>
        /// Gets whether PascalCase identifiers should be generated instead of camelCase.
        /// This is used by <see cref="ToIdentifier(string)"/>.
        /// </summary>
        public bool PascalCase => _pascalCase;

        /// <summary>
        /// Gets whether documentation should be generated.
        /// </summary>
        public bool GenerateDocumentation => _generateDocumentation;

        /// <summary>
        /// Gets or sets the <see cref="IXmlDocumentationCodeRefHandler"/> to use.
        /// When null, <see cref="DocumentationCodeRef.TextOnly"/> is used.
        /// </summary>
        public IXmlDocumentationCodeRefHandler? DocumentationCodeRefHandler { get; set; }

        /// <summary>
        /// Gets the output paths. Never empty.
        /// </summary>
        public IReadOnlyCollection<NormalizedPath> OutputPaths => _paths;

        /// <summary>
        /// Gets the root folder into which type script files must be generated.
        /// </summary>
        public TypeScriptFolder Root { get; }

        /// <summary>
        /// Gets a shared memory for this root that all <see cref="TypeScriptFolder"/>
        /// and <see cref="TypeScriptFile"/> can use.
        /// </summary>
        /// <remarks>
        /// This is better not to use this directly: hiding this shared storage behind extension methods
        /// like <see cref="TSCodeWriterDocumentationExtensions.AppendDocumentation{T}(T, IActivityMonitor, System.Reflection.MemberInfo)"/> should
        /// be done.
        /// </remarks>
        public IDictionary<object, object?> Memory => _memory ?? (_memory = new Dictionary<object, object?>());

        /// <summary>
        /// Saves this <see cref="Root"/> (all its files and creates the necessary folders)
        /// into <see cref="OutputPaths"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>True on success, false is an error occurred (the error has been logged).</returns>
        public bool Save( IActivityMonitor monitor ) => Root.Save( monitor, _paths );

        /// <summary>
        /// Ensures that an identifier follows the <see cref="PascalCase"/> configuration.
        /// Only the first character is handled.
        /// </summary>
        /// <param name="name">The identifier.</param>
        /// <returns>A formatted identifier.</returns>
        public string ToIdentifier( string name )
        {
            if( name.Length != 0 && Char.IsUpper( name, 0 ) != PascalCase )
            {
                return PascalCase
                        ? (name.Length == 1
                            ? name.ToUpperInvariant()
                            : Char.ToUpperInvariant( name[0] ) + name.Substring( 1 ))
                        : (name.Length == 1
                            ? name.ToLowerInvariant()
                            : Char.ToLowerInvariant( name[0] ) + name.Substring( 1 ));
            }
            return name;
        }

    }
}
