using CK.Core;
using CK.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using CK.EmbeddedResources;

namespace CK.Setup;

/// <summary>
/// Central class that handles TypeScript generation in a <see cref="TypeScriptRoot"/> (the <see cref="Root"/>).
/// <para>
/// This context is provided to the <see cref="ITSCodeGenerator"/> and <see cref="ITSCodeGeneratorType"/>.
/// </para>
/// </summary>
public sealed partial class TypeScriptContext
{
    readonly ICodeGenerationContext _codeContext;
    readonly TypeScriptIntegrationContext? _integrationContext;
    readonly TypeScriptBinPathAspectConfiguration _binPathConfiguration;
    readonly TSContextInitializer _initializer;
    readonly TypeScriptRoot _tsRoot;
    readonly PocoCodeGenerator _pocoGenerator;
    readonly FileSystemResourceContainer? _ckGenTransform;

    internal TypeScriptContext( ICodeGenerationContext codeCtx,
                                TypeScriptBinPathAspectConfiguration tsBinPathConfig,
                                TSContextInitializer initializer,
                                IPocoTypeNameMap? jsonExchangeableNames )
    {
        _codeContext = codeCtx;
        _integrationContext = initializer.IntegrationContext;
        _binPathConfiguration = tsBinPathConfig;
        _initializer = initializer;
        var tsConfig = tsBinPathConfig.AspectConfiguration;
        Throw.DebugAssert( tsConfig != null );
        _tsRoot = new TypeScriptRoot( tsConfig.LibraryVersions.ToImmutableDictionary(),
                                      tsConfig.PascalCase,
                                      tsConfig.GenerateDocumentation,
                                      tsConfig.IgnoreVersionsBound,
                                      initializer.RootMemory );
        _tsRoot.FolderCreated += OnFolderCreated;
        _tsRoot.TSTypes.TSFromTypeRequired += OnTSFromTypeRequired;
        _tsRoot.TSTypes.TSFromObjectRequired += OnTSFromObjectRequired;
        _tsRoot.BeforeCodeGeneration += OnBeforeCodeGeneration;
        _tsRoot.AfterDeferredCodeGeneration += OnAfterDeferredCodeGeneration;
        _tsRoot.AfterCodeGeneration += OnAfterCodeGeneration;
        Root.Root.EnsureBarrel();
        _pocoGenerator = new PocoCodeGenerator( this, initializer.TypeScriptExchangeableSet, jsonExchangeableNames );
        // The "ck-gen-transform" is not necessarily here.
        var ckGenTPath = tsBinPathConfig.TargetProjectPath.AppendPart( "ck-gen-transform" );
        // Exposes the "ck-gen-transform" container only if the folder is here.
        if( Directory.Exists( ckGenTPath ) )
        {
            _ckGenTransform = new FileSystemResourceContainer( ckGenTPath, "ck-gen-transform/" );
        }
    }

    void OnFolderCreated( TypeScriptFolder f )
    {
        if( _binPathConfiguration.Barrels.Contains( f.Path ) )
        {
            f.EnsureBarrel();
        }
    }

    /// <summary>
    /// When an object must be resolved, we simply dispatch the event to all the global ITSCodeGenerator
    /// available. It's up to them to handle it if they recognize the object.
    /// </summary>
    /// <param name="sender">The TSTypeManager.</param>
    /// <param name="e">The event with the key and the final <see cref="RequireTSFromObjectEventArgs.ResolvedType"/> to be set.</param>
    void OnTSFromObjectRequired( object? sender, RequireTSFromObjectEventArgs e )
    {
        var success = true;
        foreach( var g in _initializer.GlobalCodeGenerators )
        {
            success &= g.OnResolveObjectKey( e.Monitor, this, e );
        }
        if( success )
        {
            _pocoGenerator.OnResolveObjectKey( e.Monitor, e );
        }
    }

    /// <summary>
    /// To resolve a C# type, we first see if a configuration must be applied to it. If it's the case, the
    /// configuration is applied to the event (FolderType, TypeName, etc.).
    /// Then we call all the global ITSCodeGenerator with the event and then all the ITSCodeGeneratorType associated
    /// to the type.
    /// </summary>
    /// <param name="sender">The TSTypeManager.</param>
    /// <param name="e">The event that acts as a TSType builder.</param>
    void OnTSFromTypeRequired( object? sender, RequireTSFromTypeEventArgs e )
    {
        bool success = true;
        _initializer.RegisteredTypes.TryGetValue( e.Type, out RegisteredType regType );
        var a = regType.Attribute;
        if( a != null )
        {
            success &= e.TryInitialize( e.Monitor, a.Folder, a.FileName, a.TypeName, a.SameFolderAs, a.SameFileAs );
        }
        // Applies type generators.
        var typeGenerators = regType.Generators;
        if( typeGenerators != null )
        {
            foreach( var g in typeGenerators )
            {
                success &= g.ConfigureBuilder( e.Monitor, this, e );
            }
        }
        // Applies global generators.
        foreach( var g in _initializer.GlobalCodeGenerators )
        {
            success &= g.OnResolveType( e.Monitor, this, e );
        }
        if( success )
        {
            _pocoGenerator.OnResolveType( e.Monitor, e );
        }
        // Consider any initialization error as an error that condems the type (and will eventually
        // condemn the whole process).
        if( !success ) e.SetError();
    }

    void OnBeforeCodeGeneration( object? sender, EventMonitoredArgs e ) => BeforeCodeGeneration?.Invoke( this, e );

    void OnAfterCodeGeneration( object? sender, EventMonitoredArgs e ) => AfterCodeGeneration?.Invoke( this, e );

    void OnAfterDeferredCodeGeneration( object? sender, EventMonitoredArgs e ) => AfterDeferredCodeGeneration?.Invoke( this, e );

    /// <summary>
    /// Gets the non null <see cref="TypeScriptIntegrationContext"/> if <see cref="TypeScriptBinPathAspectConfiguration.IntegrationMode"/>
    /// is not <see cref="CKGenIntegrationMode.None"/>.
    /// </summary>
    public TypeScriptIntegrationContext? IntegrationContext => _integrationContext;

    /// <summary>
    /// Gets the <see cref="TypeScriptRoot"/>.
    /// </summary>
    public TypeScriptRoot Root => _tsRoot;

    /// <summary>
    /// Gets the <see cref="ICodeGenerationContext"/> that is being processed.
    /// </summary>
    public ICodeGenerationContext CodeContext => _codeContext;

    /// <summary>
    /// Gets the <see cref="ITSPocoCodeGenerator "/>.
    /// </summary>
    public ITSPocoCodeGenerator PocoCodeGenerator => _pocoGenerator;

    /// <summary>
    /// Gets the <see cref="TypeScriptBinPathAspectConfiguration"/>.
    /// </summary>
    public TypeScriptBinPathAspectConfiguration BinPathConfiguration => _binPathConfiguration;

    /// <summary>
    /// Gets all the global generators.
    /// </summary>
    public IReadOnlyList<ITSCodeGenerator> GlobalGenerators => _initializer.GlobalCodeGenerators;

    /// <summary>
    /// Gets the "/ck-gen-transform" container if the folder exists in
    /// the <see cref="TypeScriptBinPathAspectConfiguration.TargetProjectPath"/>.
    /// </summary>
    public IResourceContainer? CKGenTransform => _ckGenTransform;

    /// <summary>
    /// Relays the <see cref="TypeScriptRoot.BeforeCodeGeneration"/> but with this <see cref="TypeScriptContext"/>
    /// as the sender.
    /// <para>
    /// Any error or fatal emitted into <see cref="EventMonitoredArgs.Monitor"/> will be detected
    /// and will fail the code generation.
    /// </para>
    /// </summary>
    public event EventHandler<EventMonitoredArgs>? BeforeCodeGeneration;

    /// <summary>
    /// Relays the <see cref="TypeScriptRoot.AfterDeferredCodeGeneration"/> but with this <see cref="TypeScriptContext"/>
    /// as the sender.
    /// <para>
    /// Any error or fatal emitted into <see cref="EventMonitoredArgs.Monitor"/> will be detected
    /// and will fail the code generation.
    /// </para>
    /// </summary>
    public event EventHandler<EventMonitoredArgs>? AfterDeferredCodeGeneration;

    /// <summary>
    /// Relays the <see cref="TypeScriptRoot.AfterCodeGeneration"/> but with this <see cref="TypeScriptContext"/>
    /// as the sender.
    /// <para>
    /// Any error or fatal emitted into <see cref="EventMonitoredArgs.Monitor"/> will be detected
    /// and will fail the code generation.
    /// </para>
    /// </summary>
    public event EventHandler<EventMonitoredArgs>? AfterCodeGeneration;

    internal bool Run( IActivityMonitor monitor )
    {
        _tsRoot.TSTypes.RegisterStandardTypes( monitor );
        bool success;
        using( monitor.OpenInfo( $"Running TypeScript code generation for:{Environment.NewLine}{BinPathConfiguration.ToOnlyThisXml()}" ) )
        {
            success = // Initializes the global generators.
                      StartGlobalCodeGeneration( monitor, _initializer.GlobalCodeGenerators, this )
                      // Calls Root.TSTypes.ResolveType for each RegisteredType:
                      // - When the RegisteredType is a PocoType, TSTypeManager.ResolveTSType is called with the IPocoType (object resolution).
                      // - When the RegisteredType is only a C# type, TSTypeManager.ResolveTSType is called with the type (C# type resolution). 
                      && ResolveRegisteredTypes( monitor )
                      && GeneratePackageCode( monitor, _initializer.Packages, this )
                      && GenerateTSLocaleSupport( monitor, this )
                      && GenerateAssetsSupport( monitor, this )
                      // Calls the TypeScriptRoot to generate the code for all ITSFileCSharpType (run the deferred Implementors).
                      && _tsRoot.GenerateCode( monitor );
        }
        // New approach (CK-ReaDI oriented) here to manage the resources.
        // We create a ResourceSpace from the TypeScriptPackage type that exist.
        // Currently these are IRealObject but this should not be required in the future.
        var resSpaceDataBuilder = new ResourceSpaceDataBuilder();
        foreach( var p in _initializer.Packages )
        {

        }

        static bool StartGlobalCodeGeneration( IActivityMonitor monitor,
                                               ImmutableArray<ITSCodeGenerator> globals,
                                               TypeScriptContext context )
        {
            using( monitor.OpenInfo( $"Starting global code generation for the {globals.Length} {nameof( ITSCodeGenerator )} TypeScript generators." ) )
            {
                var success = true;
                foreach( var global in globals )
                {
                    using( monitor.OpenTrace( $"StartCodeGeneration for '{global.GetType():N}' global TypeScript generator." ) )
                    {
                        try
                        {
                            success &= global.StartCodeGeneration( monitor, context );
                        }
                        catch( Exception ex )
                        {
                            monitor.Error( ex );
                            success = false;
                        }
                    }
                }
                if( !success )
                {
                    monitor.CloseGroup( "Failed." );
                    return false;
                }
            }
            return true;
        }

        static bool GeneratePackageCode( IActivityMonitor monitor,
                                         ImmutableArray<TypeScriptPackageAttributeImpl> packages,
                                         TypeScriptContext context )
        {
            using( monitor.OpenInfo( $"Starting code generation for the {packages.Length} TypeScript packages." ) )
            {
                var success = true;
                foreach( var p in packages )
                {
                    try
                    {
                        success &= p.GenerateCode( monitor, context );
                    }
                    catch( Exception ex )
                    {
                        monitor.Error( ex );
                        success = false;
                    }
                }
                if( !success )
                {
                    monitor.CloseGroup( "Failed." );
                    return false;
                }
            }
            return true;
        }

        static bool GenerateAssetsSupport( IActivityMonitor monitor, TypeScriptContext context )
        {
            // First, tries to load the "ck-gen-transform/assets".
            // If there is an error loading it, give up.
            ResourceAssetSet? appAssets = null;
            if( context.CKGenTransform != null
                && !context.CKGenTransform.LoadAssets( monitor, default, out appAssets, "assets" ) )
            {
                return false;
            }
            bool success = true;
            // Creates an empty final set. 
            var f = new FinalResourceAssetSet( isPartialSet: false );
            foreach( var p in context._initializer.Packages )
            {
                if( p.Assets != null )
                {
                    success &= f.Add( monitor, p.Assets );
                }
            }
            // Now add the final override if it exists.
            if( appAssets != null )
            {
                success &= f.Add( monitor, appAssets );
            }
            if( success )
            {
                // Now we can create the final resource file in the Root and
                // adds all the assets.
                var assetsFolder = context.Root.Root.FindOrCreateFolder( "assets" );
                foreach( var (path,asset) in f.Final.Assets )
                {
                    assetsFolder.CreateResourceFile( asset.Origin, path );
                }
                context._initializer.LiveState?.SetFinalAssets( f.Final );
            }
            return success;
        }

        static bool GenerateTSLocaleSupport( IActivityMonitor monitor, TypeScriptContext context )
        {
            // First, tries to load the "ck-gen-transform/ts-locales".
            // If there is an error loading it, give up.
            LocaleCultureSet? appLocales = null;
            if( context.CKGenTransform != null
                && !context.CKGenTransform.LoadLocales( monitor,
                                                        context.BinPathConfiguration.ActiveCultures,
                                                        out appLocales,
                                                        "ts-locales",
                                                        isOverrideFolder: true ) )
            {
                return false;
            }
            bool success = true;
            // Creates an empty final culture set. 
            var f = new FinalLocaleCultureSet( isPartialSet: false, "TSLocales" );
            foreach( var p in context._initializer.Packages )
            {
                if( p.TSLocales != null )
                {
                    success &= f.Add( monitor, p.TSLocales );
                }
            }
            // Now add the final override if it exists.
            if( appLocales != null )
            {
                success &= f.Add( monitor, appLocales );
            }
            if( success )
            {
                // Now we can create the final ts-locales folder in the Root.
                var localeFolder = context.Root.Root.FindOrCreateFolder( "ts-locales" );
                WriteFinalSet( monitor, localeFolder, f );
            }
            return success;

            static void WriteFinalSet( IActivityMonitor monitor, TypeScriptFolder localeFolder, FinalLocaleCultureSet final )
            {
                final.PropagateFallbackTranslations( monitor );
                var resources = new CodeGenResourceContainer( "FinalTSLocales" );
                foreach( var set in final.Root.FlattenedAll )
                {
                    // Use the CultureInfo to have the "correct" casing for culture names.
                    var fileName = $"{set.Culture.Culture.Name}.json";
                    var res = resources.AddWriter( fileName, stream =>
                    {
                        using var w = new Utf8JsonWriter( stream, new JsonWriterOptions() { Indented = true } );
                        w.WriteStartObject();
                        foreach( var t in set.Translations )
                        {
                            w.WriteString( t.Key, t.Value.Text );
                        }
                        w.WriteEndObject();
                    } );
                    localeFolder.CreateResourceFile( res, fileName );
                }
            }
        }
    }

    bool ResolveRegisteredTypes( IActivityMonitor monitor )
    {
        bool success = true;
        Type? t = null;
        IPocoType? pT = null;
        try
        {
            using( monitor.OpenInfo( $"Declaring {_initializer.RegisteredTypes.Count} registered types." ) )
            {
                foreach( var (type, reg) in _initializer.RegisteredTypes )
                {
                    if( reg.PocoType != null )
                    {
                        pT = reg.PocoType;
                        _tsRoot.TSTypes.ResolveTSType( monitor, pT );
                    }
                    else
                    {
                        pT = null;
                        t = type;
                        _tsRoot.TSTypes.ResolveTSType( monitor, type );
                    }
                }
            }
            using( monitor.OpenInfo( $"Ensuring that all the Poco of the TypeScriptSet are registered." ) )
            {
                foreach( var p in _pocoGenerator.TypeScriptSet.NonNullableTypes )
                {
                    pT = p;
                    _tsRoot.TSTypes.ResolveTSType( monitor, pT );
                }
            }
        }
        catch( Exception ex )
        {
            success = false;
            if( pT != null )
            {
                monitor.Error( $"Unable to resolve Poco type '{pT}'.", ex );
            }
            else
            {
                monitor.Error( $"Unable to resolve type '{t:C}'.", ex );
            }
        }
        return success;
    }

}
