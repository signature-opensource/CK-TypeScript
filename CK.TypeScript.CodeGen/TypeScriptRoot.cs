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
    /// and <see cref="TypeScriptFile"/> as needed that can ultimately be <see cref="TypeScriptFolder.Save"/>d.
    /// <para>
    /// This class can be specialized in order to offer a more powerful API.
    /// </para>
    /// </summary>
    public partial class TypeScriptRoot
    {
        readonly IReadOnlyCollection<(NormalizedPath Path, XElement Config)> _pathsAndConfig;
        readonly bool _pascalCase;
        readonly bool _generateDocumentation;
        readonly bool _generatePocoInterfaces;
        Dictionary<object, object?>? _memory;

        /// <summary>
        /// Initializes a new <see cref="TypeScriptRoot"/>.
        /// </summary>
        /// <param name="pathsAndConfig">Set of output paths with their configuration element. May be empty.</param>
        /// <param name="pascalCase">Whether PascalCase identifiers should be generated instead of camelCase.</param>
        /// <param name="generateDocumentation">Whether documentation should be generated.</param>
        /// <param name="generatePocoInterfaces">Whether IPoco interfaces should be generated.</param>
        public TypeScriptRoot( IReadOnlyCollection<(NormalizedPath Path, XElement Config)> pathsAndConfig,
                               bool pascalCase,
                               bool generateDocumentation,
                               bool generatePocoInterfaces )
        {
            Throw.CheckNotNullArgument( pathsAndConfig );
            _pathsAndConfig = pathsAndConfig;
            _pascalCase = pascalCase;
            _generateDocumentation = generateDocumentation;
            _generatePocoInterfaces = generatePocoInterfaces;
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
        /// Gets whether IPoco interfaces should be generated.
        /// </summary>
        public bool GeneratePocoInterfaces => _generatePocoInterfaces;

        /// <summary>
        /// Gets or sets the <see cref="IXmlDocumentationCodeRefHandler"/> to use.
        /// When null, <see cref="DocumentationCodeRef.TextOnly"/> is used.
        /// </summary>
        public IXmlDocumentationCodeRefHandler? DocumentationCodeRefHandler { get; set; }

        /// <summary>
        /// Gets the output paths and their configuration element. Never empty.
        /// </summary>
        public IReadOnlyCollection<(NormalizedPath Path, XElement Config)> OutputPaths => _pathsAndConfig;

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
        public bool SaveTS( IActivityMonitor monitor )
        {
            var barrelPaths = _pathsAndConfig.SelectMany( c => c.Config.Elements( "Barrels" ).Elements( "Barrel" )
                                                     .Select( b => c.Path.Combine( b.Attribute( "Path" )?.Value ) ) );
            var roots = _pathsAndConfig.Select(
                p => new NormalizedPath( Path.Combine( p.Path, "ts", "src" )
            ) ).ToArray();
            var barrels = new HashSet<NormalizedPath>( roots.Concat( barrelPaths ) );// We need a root barrel for the generated module.
            return Root.Save( monitor, roots, barrels.Contains );
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
