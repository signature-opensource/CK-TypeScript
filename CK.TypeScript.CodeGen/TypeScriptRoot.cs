using CK.Core;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Central TypeScript context with options and a <see cref="Root"/> that contains as many <see cref="TypeScriptFolder"/>
    /// and <see cref="TypeScriptFile"/> as needed that can ultimately be <see cref="Save"/>d.
    /// <para>
    /// The <see cref="TSTypes"/> maps C# types to <see cref="ITSType"/>. Types can be registered directly or
    /// use the <see cref="TSTypeManager.ResolveTSType(IActivityMonitor, Type)"/> that raises <see cref="TSTypeManager.TSFromTypeRequired"/>
    /// event and registers <see cref="ITSFileCSharpType"/> that must eventually be generated when <see cref="GenerateCode(IActivityMonitor)"/>
    /// is called.
    /// </para>
    /// <para>
    /// Once code generation succeeds, <see cref="Save"/> can be called.
    /// </para>
    /// <para>
    /// This class can be used as-is or can be specialized in order to offer a more powerful API.
    /// </para>
    /// </summary>
    public class TypeScriptRoot
    {
        /// <summary>
        /// Default "@types/luxon" package version. A configured version (in <see cref="LibraryVersionConfiguration"/>)
        /// overrides this default.
        /// </summary>
        public const string LuxonTypesVersion = "3.3.7";

        /// <summary>
        /// Default "luxon" package version. A configured version (in <see cref="LibraryVersionConfiguration"/>)
        /// overrides this default.
        /// </summary>
        public const string LuxonVersion = "3.4.4";

        /// <summary>
        /// See https://mikemcl.github.io/decimal.js-light/.
        /// </summary>
        public const string DecimalJSLight = "decimal.js-light";

        /// <summary>
        /// Default "decimal.js-light" package version. A configured version (in <see cref="LibraryVersionConfiguration"/>)
        /// overrides this default.
        /// </summary>
        public const string DecimalJSLightVersion = "2.5.1";

        /// <summary>
        /// See https://mikemcl.github.io/decimal.js/.
        /// </summary>
        public const string DecimalJS = "decimal.js";

        /// <summary>
        /// Default "decimal.js" package version. A configured version (in <see cref="LibraryVersionConfiguration"/>)
        /// overrides this default.
        /// </summary>
        public const string DecimalJSVersion = "10.4.3";

        Dictionary<object, object?>? _memory;
        readonly TSTypeManager _tsTypes;
        readonly LibraryManager _libraryManager;
        readonly DocumentationBuilder _docBuilder;
        readonly bool _pascalCase;
        readonly bool _reflectTS;
        readonly string _decimalLibraryName;
        TSTypeBuilder? _firstFreeBuilder;

        /// <summary>
        /// Initializes a new <see cref="TypeScriptRoot"/>.
        /// </summary>
        /// <param name="libraryVersionConfiguration">The external library name to version mapping to use.</param>
        /// <param name="pascalCase">Whether PascalCase identifiers should be generated instead of camelCase.</param>
        /// <param name="generateDocumentation">Whether documentation should be generated.</param>
        /// <param name="reflectTS">True to generate TSType map.</param>
        /// <param name="decimalLibraryName">
        /// Support library for decimal. If <see cref="decimal"/> is used, we default to use https://github.com/MikeMcl/decimal.js-light
        /// in version <see cref="DecimalJSLightVersion"/>. if "decimal.js" is specified here, it'll be used with <see cref="DecimalJSVersion"/>.
        /// The actual version used can be overridden thanks to <paramref name="libraryVersionConfiguration"/>.
        /// <para>
        /// Other (unknown) library version MUST be provided in <paramref name="libraryVersionConfiguration"/>.
        /// </para>
        /// </param>
        public TypeScriptRoot( IReadOnlyDictionary<string, string>? libraryVersionConfiguration,
                               bool pascalCase,
                               bool generateDocumentation,
                               bool reflectTS = false,
                               string decimalLibraryName = "decimal.js-light" )
        {
            _libraryManager = new LibraryManager();
            _pascalCase = pascalCase;
            _reflectTS = reflectTS;
            _decimalLibraryName = decimalLibraryName;
            _docBuilder = new DocumentationBuilder( withStars: true, generateDoc: generateDocumentation );
            if( GetType() == typeof( TypeScriptRoot ) )
            {
                Root = new TypeScriptFolder( this );
            }
            else
            {
                var rootType = typeof( TypeScriptFolder<> ).MakeGenericType( GetType() );
                Root = (TypeScriptFolder)rootType.GetMethod( "Create", BindingFlags.NonPublic | BindingFlags.Static )!
                                                 .Invoke( null, new object[] { this } )!;
            }
            _tsTypes = new TSTypeManager( this, libraryVersionConfiguration );
        }

        /// <summary>
        /// Gets whether PascalCase identifiers should be generated instead of camelCase.
        /// This is used by <see cref="ToIdentifier(string)"/>.
        /// </summary>
        public bool PascalCase => _pascalCase;

        /// <summary>
        /// Gets whether TSType map must be generated.
        /// </summary>
        public bool ReflectTS => _reflectTS;

        /// <summary>
        /// Gets a reusable documentation builder.
        /// </summary>
        public DocumentationBuilder DocBuilder => _docBuilder;

        /// <summary>
        /// Gets the library to use for <see cref="decimal"/>.
        /// </summary>
        public string DecimalLibraryName => _decimalLibraryName;

        /// <summary>
        /// Gets the configured versions for npm packages. These configurations take precedence over the
        /// library version that can be specified by code (through <see cref="LibraryImport"/> when
        /// calling <see cref="ITSFileImportSection.EnsureImportFromLibrary(LibraryImport, string, string[])"/>).
        /// </summary>
        public IReadOnlyDictionary<string, string>? LibraryVersionConfiguration => _tsTypes._libVersionsConfig;

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
        /// Called by <see cref="OnFolderCreated(TypeScriptFolder)"/>.
        /// </summary>
        public event Action<TypeScriptFolder>? FolderCreated;

        /// <summary>
        /// Called by <see cref="OnFileCreated(TypeScriptFile)"/>.
        /// </summary>
        public event Action<TypeScriptFile>? FileCreated;

        /// <summary>
        /// Optional extension point called whenever a new folder appears.
        /// Invokes <see cref="FolderCreated"/> by default.
        /// </summary>
        /// <param name="f">The newly created folder.</param>
        internal protected virtual void OnFolderCreated( TypeScriptFolder f )
        {
            FolderCreated?.Invoke( f );
        }

        /// <summary>
        /// Optional extension point called whenever a new file appears.
        /// Invokes <see cref="FileCreated"/> by default.
        /// </summary>
        /// <param name="f">The newly created file.</param>
        internal protected virtual void OnFileCreated( TypeScriptFile f )
        {
            FileCreated?.Invoke( f );
        }

        /// <summary>
        /// Gets the TypeScript types manager.
        /// </summary>
        public TSTypeManager TSTypes => _tsTypes;

        /// <summary>
        /// Gets a <see cref="ITSTypeSignatureBuilder"/>. <see cref="ITSTypeSignatureBuilder.Build(bool)"/> must be called
        /// once and only once.
        /// </summary>
        /// <returns>A <see cref="TSBasicType"/> builder.</returns>
        public ITSTypeSignatureBuilder GetTSTypeSignatureBuilder()
        {
            var b = _firstFreeBuilder;
            if( b != null )
            {
                _firstFreeBuilder = b._nextFree;
                b._nextFree = null;
                return b;
            }
            return new TSTypeBuilder( this );
        }

        internal bool IsInPool( TSTypeBuilder b ) => _firstFreeBuilder == b || b._nextFree != null;

        internal void Return( TSTypeBuilder b )
        {
            b._nextFree = _firstFreeBuilder;
            _firstFreeBuilder = b;
        }

        /// <summary>
        /// Finds or creates a <see cref="TSManualFile"/>.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A file where TypeScript types can be created.</returns>
        public TSManualFile FindOrCreateManualFile( NormalizedPath path )
        {
            var f = Root.FindOrCreateFile( path );
            return new TSManualFile( _tsTypes, f );
        }

        /// <summary>
        /// Raised by <see cref="GenerateCode(IActivityMonitor)"/> before calling
        /// the <see cref="TSGeneratedTypeBuilder.Implementor"/> on all <see cref="ITSFileCSharpType"/>.
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
            internal AfterCodeGenerationEventArgs( IActivityMonitor monitor, IReadOnlyList<ITSFileCSharpType>? required )
                : base( monitor )
            {
                RequiredTypes = required ?? Array.Empty<ITSFileCSharpType>();
            }

            /// <summary>
            /// Gets the <see cref="ITSFileCSharpType"/> that has no <see cref="ITSFileCSharpType.TypePart"/>
            /// in their file and must be handled.
            /// </summary>
            public IReadOnlyList<ITSFileCSharpType> RequiredTypes { get; }
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
            // If BeforeCodeGeneration emits an error, we skip the whole code generation.
            // If CodeGenerator emits an error, we skip the call to AfterCodeGeneration.
            // If a TSGeneratedType.HasError is true, CodeGenerator will fail.
            using( monitor.OnError( () => success = false ) )
            {
                try
                {
                    BeforeCodeGeneration?.Invoke( this, new EventMonitoredArgs( monitor ) );
                    if( success )
                    {
                        var required = _tsTypes.GenerateCode( monitor );
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
        /// Gets the library manager.
        /// </summary>
        public LibraryManager LibraryManager => _libraryManager;

        /// <summary>
        /// Gets a shared memory for this root that all <see cref="TypeScriptFolder"/>
        /// and <see cref="TypeScriptFile"/> can use.
        /// </summary>
        /// <remarks>
        /// This is better not to use this directly: hiding this shared storage behind extension methods
        /// like <see cref="TSCodeWriterDocumentationExtensions.AppendDocumentation{T}(T, IActivityMonitor, System.Reflection.MemberInfo)"/> should
        /// be done.
        /// </remarks>
        public IDictionary<object, object?> Memory => _memory ??= new Dictionary<object, object?>();

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
