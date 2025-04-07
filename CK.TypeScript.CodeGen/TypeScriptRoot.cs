using CK.Core;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Central TypeScript context with options and a <see cref="Root"/> that contains as many <see cref="TypeScriptFolder"/>
/// and <see cref="TypeScriptFile"/> as needed that can ultimately be <see cref="Save"/>d.
/// <para>
/// The <see cref="TSTypes"/> maps C# types to <see cref="ITSType"/>. Types can be registered directly or
/// use the <see cref="TSTypeManager.ResolveTSType(IActivityMonitor, object)"/> that raises a <see cref="TSTypeManager.TSFromTypeRequired"/>
/// or <see cref="TSTypeManager.TSFromObjectRequired"/> event.
/// </para>
/// <para>
/// Once code generation succeeds, <see cref="Save"/> can be called.
/// </para>
/// </summary>
public sealed partial class TypeScriptRoot
{
    IDictionary<object, object?>? _memory;
    readonly TSTypeManager _tsTypes;
    readonly LibraryManager _libraryManager;
    readonly DocumentationBuilder _docBuilder;
    readonly TypeScriptFolder _root;
    readonly bool _pascalCase;
    readonly bool _reflectTS;

    // TSTypeBuilder support.
    readonly TypeScriptFolder _hiddenRoot;
    TSTypeBuilder? _firstFreeBuilder;
    int _tsBuilderCount;

    /// <summary>
    /// Initializes a new <see cref="TypeScriptRoot"/>.
    /// </summary>
    /// <param name="libraryVersionConfiguration">
    /// External library name to version mapping to use.
    /// This dictionary must use the <see cref="StringComparer.OrdinalIgnoreCase"/> as its <see cref="ImmutableDictionary{TKey, TValue}.KeyComparer"/>.
    /// </param>
    /// <param name="pascalCase">Whether PascalCase identifiers should be generated instead of camelCase.</param>
    /// <param name="generateDocumentation">Whether documentation should be generated.</param>
    /// <param name="ignoreVersionsBound">True to ignore npm version bound conflicts. See <see cref="LibraryManager.IgnoreVersionsBound"/>.</param>
    /// <param name="memory">Optional <see cref="Memory"/>. When let to null, this will be instanntiated on demand.</param>
    /// <param name="reflectTS">True to generate TSType map.</param>
    /// <param name="decimalLibraryName">
    /// Support library for decimal. If <see cref="decimal"/> is used, we default to use https://github.com/MikeMcl/decimal.js-light
    /// in version <see cref="LibraryManager.DecimalJSLightVersion"/>.
    /// <para>
    /// if "decimal.js" is specified here, it'll be used with <see cref="LibraryManager.DecimalJSVersion"/>.
    /// The actual version used can be overridden thanks to <paramref name="libraryVersionConfiguration"/>.
    /// </para>
    /// </param>
    public TypeScriptRoot( ImmutableDictionary<string, SVersionBound> libraryVersionConfiguration,
                           bool pascalCase,
                           bool generateDocumentation,
                           bool ignoreVersionsBound,
                           IDictionary<object, object?>? memory = null,
                           bool reflectTS = false,
                           string decimalLibraryName = "decimal.js-light" )
    {
        Throw.CheckArgument( libraryVersionConfiguration.IsEmpty || libraryVersionConfiguration.KeyComparer == StringComparer.OrdinalIgnoreCase );
        _libraryManager = new LibraryManager( libraryVersionConfiguration, decimalLibraryName, ignoreVersionsBound );
        _pascalCase = pascalCase;
        _memory = memory;
        _reflectTS = reflectTS;
        _docBuilder = new DocumentationBuilder( withStars: true, generateDoc: generateDocumentation );
        _root = new TypeScriptFolder( this );
        _tsTypes = new TSTypeManager( this );
        _hiddenRoot = new TypeScriptFolder( this );
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
    /// Gets or sets the <see cref="IXmlDocumentationCodeRefHandler"/> to use.
    /// When null, <see cref="DocumentationCodeRef.TextOnly"/> is used.
    /// </summary>
    public IXmlDocumentationCodeRefHandler? DocumentationCodeRefHandler { get; set; }

    /// <summary>
    /// Gets the root folder into which type script files must be generated.
    /// </summary>
    public TypeScriptFolder Root => _root;

    /// <summary>
    /// Raised whenever a folder is created.
    /// </summary>
    public event Action<TypeScriptFolder>? FolderCreated;

    /// <summary>
    /// Raised whenever a <see cref="TypeScriptFile"/> is created.
    /// Files from the reources are not concerned.
    /// </summary>
    public event Action<TypeScriptFile>? TypeScriptFileCreated;

    internal void OnFolderCreated( TypeScriptFolder f ) => FolderCreated?.Invoke( f );

    internal void OnTypeScriptFileCreated( TypeScriptFile f ) => TypeScriptFileCreated?.Invoke( f );

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
        return new TSTypeBuilder( _hiddenRoot, _tsBuilderCount++ );
    }

    internal bool IsInPool( TSTypeBuilder b ) => _firstFreeBuilder == b || b._nextFree != null;

    internal void Return( TSTypeBuilder b )
    {
        b._nextFree = _firstFreeBuilder;
        _firstFreeBuilder = b;
    }

    /// <summary>
    /// Raised by <see cref="GenerateCode(IActivityMonitor)"/> before calling the deferred implementors
    /// on types.
    /// <para>
    /// Any error or fatal emitted into <see cref="EventMonitoredArgs.Monitor"/> will be detected
    /// and will fail the code generation.
    /// </para>
    /// </summary>
    public event EventHandler<EventMonitoredArgs>? BeforeCodeGeneration;

    /// <summary>
    /// Raised by <see cref="GenerateCode(IActivityMonitor)"/> once deferred implementors have ran.
    /// <para>
    /// Any error or fatal emitted into <see cref="EventMonitoredArgs.Monitor"/> will be detected
    /// and will fail the code generation.
    /// </para>
    /// </summary>
    public event EventHandler<EventMonitoredArgs>? AfterDeferredCodeGeneration;

    /// <summary>
    /// Raised after the deferred implementors have successfully run on all types to implement and
    /// <see cref="AfterDeferredCodeGeneration"/> has been raised.
    /// <para>
    /// This can be used to generate pure TS support files or altering existing code but registering
    /// new types will throw an <see cref="InvalidOperationException"/>).
    /// </para>
    /// <para>
    /// If new type must be registered, use the <see cref="AfterDeferredCodeGeneration"/> event that is raised
    /// before <see cref="TSTypeManager.GenerateCodeDone"/> is set to true.
    /// </para>
    /// <para>
    /// Any error or fatal emitted into <see cref="EventMonitoredArgs.Monitor"/> will be detected
    /// and will fail the code generation.
    /// </para>
    /// </summary>
    public event EventHandler<EventMonitoredArgs>? AfterCodeGeneration;

    /// <summary>
    /// Raises the <see cref="BeforeCodeGeneration"/> event, generates the code by calling all
    /// the deferred implementors on <see cref="ITSFileCSharpType"/> and if no error has been logged,
    /// raises the <see cref="AfterDeferredCodeGeneration"/>, lock the type manager and raises
    /// the  <see cref="AfterCodeGeneration"/> event.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>True on success, false if an error occurred.</returns>
    public bool GenerateCode( IActivityMonitor monitor )
    {
        Throw.CheckState( TSTypes.GenerateCodeDone is false );
        bool success = true;
        // If BeforeCodeGeneration emits an error, we skip the whole code generation.
        // If a TSGeneratedType.HasError is true, CodeGenerator will fail.
        // If CodeGenerator emits an error, we skip the call to AfterDeferredCodeGeneration and AfterCodeGeneration.
        // If AfterDeferredCodeGeneration emits an error, we skip the call to AfterCodeGeneration.
        using( monitor.OnError( () => success = false ) )
        {
            try
            {
                EventMonitoredArgs? sameEvent = null;
                RaiseEvent( monitor, this, BeforeCodeGeneration, nameof( BeforeCodeGeneration ), ref sameEvent );
                if( success )
                {
                    var count = _tsTypes.GenerateCode( monitor );
                    if( success )
                    {
                        using( monitor.OpenInfo( $"All {count} deferred TypeScript types have been generated." ) )
                        {
                            RaiseEvent( monitor, this, AfterDeferredCodeGeneration, nameof( AfterDeferredCodeGeneration ), ref sameEvent );
                            _tsTypes.SetGeneratedCodeDone( monitor );
                            if( success )
                            {
                                RaiseEvent( monitor, this, AfterCodeGeneration, nameof( AfterCodeGeneration ), ref sameEvent );
                            }
                        }
                    }
                }
                return success;
            }
            catch( Exception ex )
            {
                monitor.Error( $"While generating TypeScript code.", ex );
                return false;
            }
        }

        static void RaiseEvent( IActivityMonitor monitor,
                                TypeScriptRoot sender,
                                EventHandler<EventMonitoredArgs>? handler,
                                string name,
                                ref EventMonitoredArgs? sameEvent )
        {
            if( handler == null )
            {
                monitor.Trace( $"Skipped raising {name} event: no subscription." );
            }
            else
            {
                using( monitor.OpenTrace( $"Raising {name} event." ) )
                {
                    try
                    {
                        handler.Invoke( sender, sameEvent ??= new EventMonitoredArgs( monitor ) );
                    }
                    catch( Exception ex )
                    {
                        monitor.Error( $"While raising {name} event.", ex );
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets the external library manager.
    /// </summary>
    public LibraryManager LibraryManager => _libraryManager;

    /// <summary>
    /// Gets a shared memory for this root that all <see cref="TypeScriptFolder"/>
    /// and <see cref="TypeScriptFile"/> can use.
    /// </summary>
    /// <remarks>
    /// This is better not to use this directly: hiding this shared storage behind extension methods
    /// is recommended (and it is even better to not use this at all).
    /// </remarks>
    public IDictionary<object, object?> Memory => _memory ??= new Dictionary<object, object?>();

    /// <summary>
    /// Publishes all files into a <see cref="ITypeScriptPublishTarget"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="target">The collector.</param>
    /// <returns>True on success, false on error (the error has been logged).</returns>
    public bool Publish( IActivityMonitor monitor, ITypeScriptPublishTarget target )
    {
        try
        {
            if( !target.Open( monitor, this ) )
            {
                return false;
            }
            var ctx = new TypeScriptFolder.PublishContext( new char[1024], target );
            _root.Publish( monitor, ref ctx );
            return target.Close( monitor, this, null );
        }
        catch( Exception ex )
        {
            monitor.Error( "Error while publishing TypeScript.", ex );
            target.Close( monitor, this, ex );
            return false;
        }
    }

    /// <summary>
    /// Ensures that an identifier follows the <see cref="PascalCase"/> configuration.
    /// Only the first character is handled.
    /// </summary>
    /// <param name="name">The identifier.</param>
    /// <returns>A formatted identifier.</returns>
    public string ToIdentifier( string name ) => ToIdentifier( name, PascalCase );

    /// <summary>
    /// Ensures that an identifier follows the C# PascalCase xor camelCase convention.
    /// When <paramref name="pascalCase"/> is false, <see cref="JsonNamingPolicy.CamelCase"/> is used.
    /// </summary>
    /// <param name="name">The identifier.</param>
    /// <param name="pascalCase">The target casing.</param>
    /// <returns>A formatted identifier.</returns>
    public static string ToIdentifier( string name, bool pascalCase )
    {
        return pascalCase
                ? name
                : JsonNamingPolicy.CamelCase.ConvertName( name );
    }

}
