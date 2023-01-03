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
    /// The <see cref="TSTypes"/> maps C# types to <see cref="ITSType"/>. Types can be registered directly or
    /// use the <see cref="TSTypeManager.ResolveTSType(IActivityMonitor, Type)"/> that raises <see cref="TSTypeManager.BuilderRequired"/>
    /// event and registers <see cref="ITSGeneratedType"/> that must eventually be generated when <see cref="GenerateCode(IActivityMonitor)"/>
    /// is called.
    /// </para>
    /// <para>
    /// Actual code generation is done either by the <see cref="TSGeneratedTypeBuilder.Implementor"/> for each <see cref="ITSGeneratedType"/>
    /// or during <see cref="BeforeCodeGeneration"/> or <see cref="AfterCodeGeneration"/> events.
    /// </para>
    /// <para>
    /// Once code generation succeeds, <see cref="SaveTS(IActivityMonitor)"/> can be called.
    /// </para>
    /// <para>
    /// This class can be specialized in order to offer a more powerful API.
    /// </para>
    /// </summary>
    public partial class TypeScriptGenerator
    {
        readonly IReadOnlyCollection<(NormalizedPath Path, XElement Config)> _pathsAndConfig;
        readonly bool _pascalCase;
        readonly bool _generateDocumentation;
        Dictionary<object, object?>? _memory;

        /// <summary>
        /// Initializes a new <see cref="TypeScriptGenerator"/>.
        /// </summary>
        /// <param name="pathsAndConfig">Set of output paths with their configuration element. May be empty.</param>
        /// <param name="libraryVersionConfiguration">The external library name to version mapping to use.</param>
        /// <param name="pascalCase">Whether PascalCase identifiers should be generated instead of camelCase.</param>
        /// <param name="generateDocumentation">Whether documentation should be generated.</param>
        public TypeScriptGenerator( IReadOnlyCollection<(NormalizedPath Path, XElement Config)> pathsAndConfig,
                                    IReadOnlyDictionary<string, string>? libraryVersionConfiguration,
                                    bool pascalCase,
                                    bool generateDocumentation )
        {
            Throw.CheckNotNullArgument( pathsAndConfig );
            _pathsAndConfig = pathsAndConfig;
            _pascalCase = pascalCase;
            _generateDocumentation = generateDocumentation;
            TSTypes = new TSTypeManager( this, libraryVersionConfiguration );
            if( GetType() == typeof( TypeScriptGenerator ) )
            {
                Root = new TypeScriptFolder( this );
            }
            else
            {
                var rootType = typeof( TypeScriptFolder<> ).MakeGenericType( GetType() );
                Root = (TypeScriptFolder)rootType.GetMethod( "Create", BindingFlags.NonPublic | BindingFlags.Static )!
                                                 .Invoke( null, new object[] { this } )!;
            }
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
        /// Gets the output paths and their configuration element. Never empty.
        /// </summary>
        public IReadOnlyCollection<(NormalizedPath Path, XElement Config)> OutputPaths => _pathsAndConfig;

        /// <summary>
        /// Gets the root folder into which type script files must be generated.
        /// </summary>
        public TypeScriptFolder Root { get; }

        /// <summary>
        /// Gets the TypeScript types manager.
        /// </summary>
        public TSTypeManager TSTypes { get; }

        /// <summary>
        /// Raised by <see cref="GenerateCode(IActivityMonitor)"/> before calling
        /// the <see cref="TSGeneratedTypeBuilder.Implementor"/> on all <see cref="ITSGeneratedType"/>.
        /// </summary>
        public event EventHandler<EventMonitoredArgs>? BeforeCodeGeneration;

        /// <summary>
        /// Raised after the <see cref="TSGeneratedTypeBuilder.Implementor"/> have run on all
        /// types to implement.
        /// </summary>
        public event EventHandler<AfterCodeGenerationEventArgs>? AfterCodeGeneration;

        /// <summary>
        /// Event raised by <see cref="AfterCodeGeneration"/> event.
        /// </summary>
        public sealed class AfterCodeGenerationEventArgs : EventMonitoredArgs
        {
            internal AfterCodeGenerationEventArgs( IActivityMonitor monitor, IReadOnlyList<ITSGeneratedType>? required )
                : base( monitor )
            {
                RequiredTypes = required ?? Array.Empty<ITSGeneratedType>();
            }

            /// <summary>
            /// Gets the <see cref="ITSGeneratedType"/> that has no <see cref="ITSGeneratedType.TypePart"/>
            /// in their file and must be handled.
            /// </summary>
            public IReadOnlyList<ITSGeneratedType> RequiredTypes { get; }
        }

        /// <summary>
        /// Raises the <see cref="BeforeCodeGeneration"/> event, generates the code by calling all
        /// the <see cref="TSGeneratedTypeBuilder.Implementor"/> and if no error has been logged,
        /// raises the <see cref="AfterCodeGeneration"/> event.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>True on success, false if an error occurred.</returns>
        public bool GenerateCode( IActivityMonitor monitor )
        {
            bool success = true;
            using( monitor.OnError( () => success = false ) )
            {
                try
                {
                    BeforeCodeGeneration?.Invoke( this, new EventMonitoredArgs( monitor ) );
                    if( success )
                    {
                        var required = TSTypes.GenerateCode( monitor );
                        if( success )
                        {
                            if( required == null )
                            {
                                monitor.Info( "All TypeScript Types have been generated." );
                            }
                            else
                            {
                                monitor.Warn( $"{required.Count} TypeScript Types have not been generated." );
                            }
                            AfterCodeGeneration?.Invoke( this, new AfterCodeGenerationEventArgs( monitor, required ) );
                        }
                    }
                    return true;
                }
                catch( Exception ex )
                {
                    monitor.Error( $"While generating TypeScript code.", ex );
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets a shared memory for this generator that all <see cref="TypeScriptFolder"/>
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
            var roots = _pathsAndConfig.Select( p => p.Path.Combine( "ts/src" ) ).ToArray();
            // We need a root barrel for the generated module: the roots are in the barrels.
            var barrels = new HashSet<NormalizedPath>( roots.Concat( barrelPaths ) );
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
