using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Central TypeScript context with options and a <see cref="Root"/> that contains as many <see cref="TypeScriptFolder"/>
    /// and <see cref="TypeScriptFile"/> as needed that can ultimately be <see cref="Save"/>d.
    /// <para>
    /// This class can be specialized in order to offer a more powerful API.
    /// </para>
    /// </summary>
    public class TypeScriptRoot
    {
        readonly bool _pascalCase;
        readonly bool _generateDocumentation;
        Dictionary<object, object?>? _memory;

        /// <summary>
        /// Initializes a new <see cref="TypeScriptRoot"/>.
        /// </summary>
        /// <param name="pascalCase">Whether PascalCase identifiers should be generated instead of camelCase.</param>
        /// <param name="generateDocumentation">Whether documentation should be generated.</param>
        public TypeScriptRoot( bool pascalCase,
                               bool generateDocumentation )
        {
            _pascalCase = pascalCase;
            _generateDocumentation = generateDocumentation;
            var rootType = typeof( TypeScriptFolder<> ).MakeGenericType( GetType() );
            Root = (TypeScriptFolder)rootType.GetMethod( "Create", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static )!
                                             .Invoke( null, new object[] { this } )!;
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
        /// Gets the root folder into which type script files must be generated.
        /// </summary>
        public TypeScriptFolder Root { get; }

        /// <summary>
        /// Optional extension point called whenever a new folder appears.
        /// Does nothing by default.
        /// </summary>
        /// <param name="f">The newly created folder.</param>
        internal protected virtual void OnFolderCreated( TypeScriptFolder f )
        {
        }

        /// <summary>
        /// Optional extension point called whenever a new file appears.
        /// Does nothing by default.
        /// </summary>
        /// <param name="f">The newly created file.</param>
        internal protected virtual void OnFileCreated( TypeScriptFile f )
        {
        }

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
        /// into <paramref name="outputPath"/>, ensuring that a barrel will be generated for the <see cref="Root"/>
        /// folder.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="outputPath">The target output folder.</param>
        /// <param name="previousPaths">
        /// Optional set of file paths from which actually saved paths will be removed:
        /// what's left will be the actual generated paths.
        /// </param>
        /// <returns>Number of files saved on success, null if an error occurred (the error has been logged).</returns>
        public int? Save( IActivityMonitor monitor, NormalizedPath outputPath, HashSet<string>? previousPaths = null )
        {
            // We need a root barrel for the generated module.
            Root.EnsureBarrel();
            return Root.Save( monitor, outputPath, previousPaths );
        }

        /// <summary>
        /// Ensures that an identifier follows the <see cref="PascalCase"/> configuration.
        /// Only the first character is handled.
        /// </summary>
        /// <param name="name">The identifier.</param>
        /// <returns>A formatted identifier.</returns>
        public string ToIdentifier( string name ) => ToIdentifier( name, PascalCase );

        /// <summary>
        /// Ensures that an identifier follows the PascalCase xor camelCase convention.
        /// Only the first character is handled.
        /// </summary>
        /// <param name="name">The identifier.</param>
        /// <param name="pascalCase">The target casing.</param>
        /// <returns>A formatted identifier.</returns>
        public static string ToIdentifier( string name, bool pascalCase )
        {
            if( name.Length != 0 && Char.IsUpper( name, 0 ) != pascalCase )
            {
                return pascalCase
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
