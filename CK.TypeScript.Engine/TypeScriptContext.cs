using CK.Core;
using CK.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using CK.Transform.Core;
using CK.TypeScript.Transform;
using CK.Html.Transform;
using CK.Less.Transform;

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
    readonly ActiveCultureSet _activeCultures;
    readonly TSContextInitializer _initializer;
    readonly TypeScriptRoot _tsRoot;
    readonly PocoCodeGenerator _pocoGenerator;

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
        _activeCultures = new ActiveCultureSet( tsBinPathConfig.ActiveCultures );
        _tsRoot.FolderCreated += OnFolderCreated;
        _tsRoot.TSTypes.TSFromTypeRequired += OnTSFromTypeRequired;
        _tsRoot.TSTypes.TSFromObjectRequired += OnTSFromObjectRequired;
        _tsRoot.BeforeCodeGeneration += OnBeforeCodeGeneration;
        _tsRoot.AfterDeferredCodeGeneration += OnAfterDeferredCodeGeneration;
        _tsRoot.AfterCodeGeneration += OnAfterCodeGeneration;
        // We want a root barrel for the ck-gen/ module.
        Root.Root.EnsureBarrel();
        _pocoGenerator = new PocoCodeGenerator( this, initializer.TypeScriptExchangeableSet, jsonExchangeableNames );
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
    /// Gets the active cultures.
    /// </summary>
    public ActiveCultureSet ActiveCultures => _activeCultures;

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
        using var _ = monitor.OpenInfo( $"Running TypeScript code generation for:{Environment.NewLine}{BinPathConfiguration.ToOnlyThisXml()}" );

        _tsRoot.TSTypes.RegisterStandardTypes( monitor );
        bool success = true;

        var typeScriptContext = this;

        // New approach (CK-ReaDI oriented) here to manage the resources.
        var resSpaceConfiguration = new ResourceSpaceConfiguration();
        resSpaceConfiguration.AppResourcesLocalPath = _binPathConfiguration.TargetProjectPath.AppendPart( "ck-gen-app" );

        // Some GlobalCodeGenerators may generate TypeScript types that are required by package registrations:
        // this is the case of the Angular engine that injects its AngularCodeGen instance in the awful Root.Memory
        // and NgComponent registers themselves on it.
        // So we call StartGlobalCodeGeneration now...
        // If another GlobalCodeGenerators needs the ResPackage, we must add a new method on ITSCodeGenerator (something like
        // "OnPackageReady") and call it. This imperative paradigm is intrinsically limited...
        // ReaDI handles this transparently: the global generators can be any ReaDI object with (IActivityMonitor, TypeScriptContext)
        // parameters... And if the same object wants to generate code in the TypeScriptContext based on the topologically ordered
        // set of packages, another method with (IActivityMonitor, TypeScriptContext, ResourceSpaceData) can be written.
        //
        success = StartGlobalCodeGeneration( monitor, _initializer.GlobalCodeGenerators, typeScriptContext );

        // ResPackageDescriptor can be registered on the initial ResourceSpaceConfiguration (but also on the
        // following ResourceSpaceCollector). By registering our discovered TypeScriptPackage here, we maximize
        // the possibilities for other participants to use them.
        // We manually trigger TypeScriptPackage registration.
        success = ConfigureResPackages( monitor, resSpaceConfiguration, _initializer.Packages, typeScriptContext );

        var resSpaceCollector = resSpaceConfiguration.Build( monitor );
        if( resSpaceCollector == null ) return false;


        // With CK-ReaDI, publishing this ResourceSpaceCollector here will allow
        // any code to register additional packages in it, including totally "virtual"
        // ones that could produce .ts or other resources based on an existing resource that is
        // a schema or any description of code.

        // Today, the GlobalCodeGenerators have no access to the resources.
        // With CK-ReaDI, they could have a similar ConfigureResPackages. They would then be able
        // to create new ResPackage in addition to (as of today) registering import libraries, creating
        // TypeScriptFile and registering to events (that should ideally not exist...).
        // if( success ) success = StartGlobalCodeGeneration( monitor, _initializer.GlobalCodeGenerators, typeScriptContext );

        // Resolving registered types:
        // Calls Root.TSTypes.ResolveType for each RegisteredType:
        // - When the RegisteredType is a PocoType, TSTypeManager.ResolveTSType is called with the IPocoType (object resolution).
        // - When the RegisteredType is only a C# type, TSTypeManager.ResolveTSType is called with the type (C# type resolution).
        //
        // This event based approach works but is not ideal. The problem is that Type resolution needs "defferring" and some
        // modularity for more than one piece of code to resolve and implement a type.
        // This may be replaced with a RegisteredTypes ReaDI object but this is not obvious.
        if( success ) success = ResolveRegisteredTypes( monitor );

        // This is the last step that generates all the TypeScriptFiles that must be generated (runs all
        // the deferred Implementors).
        // This raises events and closes Type registration (generates the code for all ITSFileCSharpType, running
        // the deferred Implementors).
        // 
        if( !success || !_tsRoot.GenerateCode( monitor ) )
        {
            // Time to give up.
            return false;
        }

        var dataSpaceBuilder = new ResourceSpaceDataBuilder( resSpaceCollector );
        var spaceData = dataSpaceBuilder.Build( monitor );
        if( spaceData == null ) return false;

        // When a ResourceSpaceData is available, we are almost done.
        // It exposes all the read only packages including the head "<Code>" and tail "<App>" packages
        // topologically ordered.
        // The "<Code>" may still be empty: setting the GeneratedCodeContainer can be defferred up to
        // the ResourceSpaceBuilder.

        // On the ResourceSpaceBuilder, resource handlers can now be registered before building the
        // final ResourceSpace.

        var installer = new InitialFileSystemInstaller( _binPathConfiguration.TargetProjectPath.AppendPart( "ck-gen" ) );
        var spaceBuilder = new ResourceSpaceBuilder( spaceData );
        // Last chance to publish the TypeScript files in a GeneratedCodeContainer and
        // assign it to the resource space.
        var codeTarget = new CodeGenResourceContainerTarget();
        if( !_tsRoot.Publish( monitor, codeTarget ) )
        {
            return false;
        }
        Throw.DebugAssert( codeTarget.Result != null );
        spaceBuilder.GeneratedCodeContainer = codeTarget.Result;


        success &= spaceBuilder.RegisterHandler( monitor, new AssetsResourceHandler( installer, spaceData.ResPackageDataCache, "ts-assets" ) );
        success &= spaceBuilder.RegisterHandler( monitor, new LocalesResourceHandler( installer,
                                                                                         spaceData.ResPackageDataCache,
                                                                                         "ts-locales",
                                                                                         typeScriptContext.ActiveCultures,
                                                                                         LocalesResourceHandler.InstallOption.Full ) );
        var transformerHost = new TransformerHost( new TypeScriptLanguage(), new HtmlLanguage(), new LessLanguage() );
        success &= spaceBuilder.RegisterHandler( monitor, new TransformableFileHandler( installer, transformerHost ) );

        var resSpace = spaceBuilder.Build( monitor );
        if( resSpace == null ) return false;
        //
        // Currently, Install must be called. We should either:
        // 1 - Integrate the Install to the ResourceSpaceBuilder.Build().
        // 2 - Introduce a ResourceSpaceInstaller builder.
        // But because of the "handler => installer" rule, 1 seems the good choice... but it seems odd to not have
        // a distinct phasis for the install...
        // 
        return resSpace.Install( monitor );


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

        static bool ConfigureResPackages( IActivityMonitor monitor,
                                          ResourceSpaceConfiguration resourcesConfiguration,
                                          IReadOnlyList<TypeScriptGroupOrPackageAttributeImpl> packages,
                                          TypeScriptContext context )
        {
            using( monitor.OpenInfo( $"Configuring {packages.Count} TypeScript resource packages." ) )
            {
                var success = true;
                foreach( var p in packages )
                {
                    try
                    {
                        success &= p.ConfigureResDescriptor( monitor, context, resourcesConfiguration );
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
