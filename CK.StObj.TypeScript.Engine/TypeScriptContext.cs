using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;

namespace CK.Setup
{
    /// <summary>
    /// Central class that handles TypeScript generation in a <see cref="TypeScriptRoot"/> (the <see cref="Root"/>)
    /// and <see cref="ICodeGenerationContext"/> (the <see cref="CodeContext"/>).
    /// <para>
    /// This is instantiated and made available to the participants (<see cref="ITSCodeGenerator"/> and <see cref="ITSCodeGeneratorType"/>)
    /// only if the configuration actually allows the TypeScript generation for this <see cref="CodeContext"/>.
    /// </para>
    /// </summary>
    public sealed class TypeScriptContext
    {
        readonly ICodeGenerationContext _codeContext;
        readonly IReadOnlyDictionary<Type, ITypeAttributesCache> _attributeCache;
        readonly TypeScriptAspectConfiguration _configuration;
        readonly TypeScriptAspectBinPathConfiguration _binPathConfiguration;
        readonly IPocoTypeSystem _pocoTypeSystem;
        readonly ExchangeableTypeNameMap? _jsonNames;
        readonly TypeScriptRoot _tsRoot;

        readonly Dictionary<Type, RegType> _registeredTypes;
        readonly Dictionary<Type, TypeScriptAttribute> _fromConfiguration;
        readonly List<ITSCodeGenerator> _globals;
        bool _success;

        readonly struct RegType
        {
            public RegType( IReadOnlyList<ITSCodeGeneratorType>? generators, TypeScriptAttribute? attr )
            {
                Generators = generators;
                Attribute = attr;
            }

            public readonly TypeScriptAttribute? Attribute;

            public readonly IReadOnlyList<ITSCodeGeneratorType>? Generators;
        }

        internal TypeScriptContext( ICodeGenerationContext codeCtx,
                                    TypeScriptAspectConfiguration tsConfig,
                                    TypeScriptAspectBinPathConfiguration tsBinPathConfig,
                                    IPocoTypeSystem pocoTypeSystem,
                                    ExchangeableTypeNameMap? jsonNames )
        {
            _codeContext = codeCtx;
            _configuration = tsConfig;
            _binPathConfiguration = tsBinPathConfig;
            _pocoTypeSystem = pocoTypeSystem;
            _jsonNames = jsonNames;
            _tsRoot = new TypeScriptRoot( tsConfig.LibraryVersions, tsConfig.PascalCase, tsConfig.GenerateDocumentation );
            _tsRoot.FolderCreated += OnFolderCreated;
            _tsRoot.TSTypes.TypeBuilderRequired += OnTypeBuilderRequired;
            _tsRoot.TSTypes.TSTypeRequired += OnTSTypeRequired;
            _registeredTypes = new Dictionary<Type, RegType>();
            _fromConfiguration = new Dictionary<Type, TypeScriptAttribute>();
            _attributeCache = codeCtx.CurrentRun.EngineMap.AllTypesAttributesCache;
            _globals = new List<ITSCodeGenerator>();
            _success = true;
            Root.Root.EnsureBarrel();
        }

        void OnFolderCreated( TypeScriptFolder f )
        {
            if( _binPathConfiguration.Barrels.Contains( f.Path ) )
            {
                f.EnsureBarrel();
            }
        }

        void OnTSTypeRequired( object? sender, TSTypeRequiredEventArgs e )
        {
            foreach( var g in _globals )
            {
                g.OnResolveObjectKey( e.Monitor, this, e );
            }
        }

        void OnTypeBuilderRequired( object? sender, TypeBuilderRequiredEventArgs e )
        {
            bool success = true;
            _registeredTypes.TryGetValue( e.Type, out RegType regType );
            var a = regType.Attribute;
            if( a != null )
            {
                success &= e.TryInitialize( e.Monitor, a.Folder, a.FileName, a.TypeName, a.SameFolderAs, a.SameFileAs );
            }
            // Applies global generators.
            foreach( var g in _globals )
            {
                success &= g.ConfigureBuilder( e.Monitor, this, e );
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
            // Consider any initialization error as an error that condems the type (and eventually the whole process).
            if( !success ) e.SetError();
        }

        /// <summary>
        /// Gets the <see cref="TypeScriptRoot"/>.
        /// </summary>
        public TypeScriptRoot Root => _tsRoot;

        /// <summary>
        /// Gets the <see cref="ICodeGenerationContext"/> that is being processed.
        /// </summary>
        public ICodeGenerationContext CodeContext => _codeContext;

        /// <summary>
        /// Gets the <see cref="TypeScriptAspectConfiguration"/>.
        /// </summary>
        public TypeScriptAspectConfiguration Configuration => _configuration;

        /// <summary>
        /// Gets the <see cref="TypeScriptAspectBinPathConfiguration"/>.
        /// </summary>
        public TypeScriptAspectBinPathConfiguration BinPathConfiguration => _binPathConfiguration;

        /// <summary>
        /// Gets the Json <see cref="ExchangeableTypeNameMap"/> if it is available.
        /// </summary>
        public ExchangeableTypeNameMap? JsonNames => _jsonNames;

        /// <summary>
        /// Gets all the global generators.
        /// </summary>
        public IReadOnlyList<ITSCodeGenerator> GlobalGenerators => _globals;

        /// <summary>
        /// Gets the Poco code generator (the first <see cref="GlobalGenerators"/>).
        /// </summary>
        public PocoCodeGenerator PocoCodeGenerator => (PocoCodeGenerator)_globals[0];

        internal bool Run( IActivityMonitor monitor )
        {
            _tsRoot.TSTypes.RegisterStandardTypes( monitor );
            var pocoDirectory = CodeContext.CurrentRun.ServiceContainer.GetRequiredService<IPocoDirectory>();
            var pocoTypeSystem = CodeContext.CurrentRun.ServiceContainer.GetRequiredService<IPocoTypeSystem>();
            using( monitor.OpenInfo( $"Running TypeScript code generation for:{Environment.NewLine}{BinPathConfiguration.ToXml()}" ) )
            {
                return // Projects the BinPathConfiguration.Types in RegType.Attribute.
                        BuildRegTypesFromConfiguration( monitor, pocoDirectory )
                        // Discovering the globals ITSCodeGenerator, type bound ITSCodeGenerator and TypeScript attributes.
                        // - Registers the globals,
                        // - Type bound generators are registered in RegType.Generators,
                        // - TypeScript atributes are stored in RegType.Attribute (if the type appeared in BinPathConfiguration.Types,
                        //   the configured values override the code values).
                        && BuildRegTypesFromAttributesAndDiscoverGenerators( monitor, pocoTypeSystem )
                        // Initializes the global generators.
                        && CallGlobalCodeGenerators( monitor, initialize: true )
                        // Calls Root.TSTypes.ResolveType for each non null RegType.Attribute.
                        && ResolveRegisteredTypes( monitor )
                        // Calls the TypeScriptRoot to generate the code for all ITSGeneratedType.
                        && _tsRoot.GenerateCode( monitor )
                        // Runs the global generators GenerateCode.
                        && CallGlobalCodeGenerators( monitor, false );
            }
        }


        bool BuildRegTypesFromConfiguration( IActivityMonitor monitor, IPocoDirectory directory )
        {
            using( monitor.OpenInfo( $"Building TypeScriptAttribute for {BinPathConfiguration.Types.Count} Type configurations." ) )
            {
                bool success = true;
                foreach( TypeScriptTypeConfiguration c in _binPathConfiguration.Types )
                {
                    Type? t = FindType( directory, c.Type );
                    if( t == null )
                    {
                        monitor.Error( $"Unable to resolve type '{c.Type}' in TypeScriptAspectConfiguration in:{Environment.NewLine}{c.ToXml()}" );
                        success = false;
                    }
                    else
                    {
                        var attr = c.ToAttribute( monitor, ( monitor, typeName ) => FindType( directory, typeName ) );
                        if( attr == null ) success = false;
                        else
                        {
                            _fromConfiguration.Add( t, attr );
                            _registeredTypes.Add( t, new RegType( null, attr ) );
                        }
                    }
                }
                monitor.CloseGroup( $"{_fromConfiguration.Count} configurations processed." );
                return success;
            }

            static Type? FindType( IPocoDirectory d, string typeName )
            {
                var t = SimpleTypeFinder.WeakResolver( typeName, false );
                if( t == null )
                {
                    if( d.NamedFamilies.TryGetValue( typeName, out var rootInfo ) )
                    {
                        t = rootInfo.PrimaryInterface.PocoInterface;
                    }
                }
                return t;
            }

        }

        bool BuildRegTypesFromAttributesAndDiscoverGenerators( IActivityMonitor monitor, IPocoTypeSystem pocoTypeSystem )
        {
            _globals.Add( new PocoCodeGenerator( pocoTypeSystem ) );
            _globals.Add( new GlobalizationTypesCodeGenerator() );

            using( monitor.OpenInfo( "Analyzing types with [TypeScript] and/or ITSCodeGeneratorType or ITSCodeGenerator attributes." ) )
            {
                // These variables are reused per type.
                TypeScriptAttributeImpl? impl;
                List<ITSCodeGeneratorType> generators = new List<ITSCodeGeneratorType>();

                foreach( ITypeAttributesCache attributeCache in _attributeCache.Values )
                {
                    impl = null;
                    generators.Clear();

                    foreach( var m in attributeCache.GetTypeCustomAttributes<ITSCodeGeneratorAutoDiscovery>() )
                    {
                        if( m is ITSCodeGenerator g )
                        {
                            _globals.Add( g );
                        }
                        if( m is TypeScriptAttributeImpl a )
                        {
                            if( impl != null )
                            {
                                monitor.Error( $"Multiple TypeScriptAttribute decorates '{attributeCache.Type}'." );
                                _success = false;
                            }
                            impl = a;
                        }
                        if( m is ITSCodeGeneratorType tG )
                        {
                            generators.Add( tG );
                        }
                    }
                    // If the attribute is only a ITSCodeGeneratorType, we don't consider it as an empty TypeScriptAttribute,
                    // we register it if and only if it appears in the configuration (we let the TSDecoratedType.Attribute be null).
                    // And if it is not declared in the configuration we keep the array of its generators to be able to
                    // use them if the type is referenced by another referenced type.
                    if( impl != null || generators.Count > 0 )
                    {
                        // Did this type appear in the configuration?
                        // If yes, the configuration must override the values from the code.
                        var configuredAttr = _registeredTypes.GetValueOrDefault( attributeCache.Type ).Attribute;
                        var a = impl?.Attribute.ApplyOverride( configuredAttr ) ?? configuredAttr;
                        _registeredTypes[attributeCache.Type] = new RegType( generators.Count > 0 ? generators.ToArray() : null, a );
                    }
                }
                if( _success ) monitor.CloseGroup( $"Found {_globals.Count} global generators and {_registeredTypes.Count} types to consider." );
                return _success;
            }
        }

        bool ResolveRegisteredTypes( IActivityMonitor monitor )
        {
            using( monitor.OpenInfo( $"Declaring registered types." ) )
            {
                foreach( var (type, dec) in _registeredTypes )
                {
                    if( dec.Attribute != null )
                    {
                        _tsRoot.TSTypes.ResolveTSType( monitor, type );
                    }
                }
                return true;
            }
        }

        bool CallGlobalCodeGenerators( IActivityMonitor monitor, bool initialize )
        {
            string action = initialize ? "Initializing" : "Executing";
            Debug.Assert( _success );
            // Executes all the globals.
            using( monitor.OpenInfo( $"{action} the {_globals.Count} global {nameof(ITSCodeGenerator)} TypeScript generators." ) )
            {
                foreach( var global in _globals )
                {
                    using( monitor.OpenTrace( $"{action} '{global.GetType().FullName}' global TypeScript generator." ) )
                    {
                        try
                        {
                            _success = initialize
                                        ? global.Initialize( monitor, this )
                                        : global.GenerateCode( monitor, this );
                        }
                        catch( Exception ex )
                        {
                            monitor.Error( ex );
                            _success = false;
                        }
                        if( !_success )
                        {
                            monitor.CloseGroup( "Failed." );
                            return false;
                        }
                    }
                }
            }
            return _success;
        }

        internal bool Save( IActivityMonitor monitor )
        {
            bool success = true;
            using( monitor.OpenInfo( $"Saving generated TypeScript for:{Environment.NewLine}{BinPathConfiguration.ToXml()}" ) )
            {
                var ckGenFolder = BinPathConfiguration.TargetProjectPath.AppendPart( "ck-gen" );
                var ckGenFolderSrc = ckGenFolder.AppendPart( "src" );
                // To minimize impacts on file watchers, we don't destroy/recreate the ck-gen folder.
                // Instead we update the existing files in place and then remove any paths that have not
                // been generated.
                HashSet<string>? cleanupPaths = null;
                if( Directory.Exists( ckGenFolderSrc ) )
                {
                    var previous = Directory.EnumerateFiles( ckGenFolderSrc, "*", SearchOption.AllDirectories );
                    if( Path.DirectorySeparatorChar != NormalizedPath.DirectorySeparatorChar )
                    {
                        previous = previous.Select( p => p.Replace( Path.DirectorySeparatorChar, NormalizedPath.DirectorySeparatorChar ) );
                    }
                    cleanupPaths = new HashSet<string>( previous );
                    monitor.Trace( $"Found {cleanupPaths.Count} existing files in '{ckGenFolderSrc}'." );
                }
                int? savedCount = Root.Save( monitor, ckGenFolderSrc, cleanupPaths );
                if( savedCount.HasValue )
                {
                    if( savedCount.Value == 0 )
                    {
                        monitor.Warn( $"No files or folders have been generated in '{ckGenFolder}'. Skipping TypeScript generation." );
                    }
                    else
                    {
                        if( BinPathConfiguration.GitIgnoreCKGenFolder )
                        {
                            File.WriteAllText( Path.Combine( ckGenFolder, ".gitignore" ), "*" );
                        }
                        // Preload the target package.json if it exists to extract the typescriptVersion if it exists.
                        // Even if the target package is on error or has no typescript installed, we CAN build the generated
                        // typescript by using the latest typescript.
                        var targetProjectPath = BinPathConfiguration.TargetProjectPath;
                        var projectJsonPath = targetProjectPath.AppendPart( "package.json" );
                        var targetPackageJson = YarnHelper.LoadPackageJson( monitor, projectJsonPath, out bool invalidPackageJson );
                        var targetTypescriptVersion = targetPackageJson?["devDependencies"]?["typescript"]?.ToString();
                        if( targetTypescriptVersion != null )
                        {
                            monitor.Info( $"Found typescript in version '{targetTypescriptVersion}' in target '{projectJsonPath}'." );
                        }
                        else 
                        {
                            if( !invalidPackageJson )
                            {
                                if( targetPackageJson == null )
                                {
                                    monitor.Info( "No target package.json found, we'll install typescript current version in '@local/ck-gen'." );
                                }
                                else
                                {
                                    monitor.Warn( $"Typescript is not installed in target '{projectJsonPath}', we'll install typescript current version in '@local/ck-gen'." );
                                }
                            }
                        }
                        // Generates "/ck-gen" files "package.json", "tsconfig.json" and "tsconfig-cjs.json".
                        // This may fail if there's an error in the dependencies declared by the code
                        // generator (in LibraryImport).
                        if( YarnHelper.SaveCKGenBuildConfig( monitor, ckGenFolder, targetTypescriptVersion, this ) )
                        {
                            if( BinPathConfiguration.SkipTypeScriptTooling )
                            {
                                monitor.Info( "Skipping any TypeScript project setup since SkipTypeScriptTooling is true." );
                            }
                            else
                            {
                                var yarnPath = YarnHelper.GetYarnInstallPath( monitor,
                                                                              targetProjectPath,
                                                                              BinPathConfiguration.AutoInstallYarn );
                                if( yarnPath.HasValue )
                                {
                                    // We have a yarn, we can build "@local/ck-gen".
                                    using( monitor.OpenInfo( $"Building '@local/ck-gen' package..." ) )
                                    {
                                        if( targetTypescriptVersion == null )
                                        {
                                            success &= YarnHelper.DoRunYarn( monitor, ckGenFolder, "add --dev typescript", yarnPath.Value );
                                        }
                                        else
                                        {
                                            success &= YarnHelper.DoRunYarn( monitor, ckGenFolder, "install", yarnPath.Value );
                                        }
                                        success &= YarnHelper.DoRunYarn( monitor, ckGenFolder, "run build", yarnPath.Value );
                                        monitor.CloseGroup( success ? "Success." : "Failed." );
                                    }

                                    // If the lookup made previously to the target package.json is not on error, we handle
                                    // AutoInstallVSCodeSupport and EnsureTestSupport.
                                    // We do this even on compilation failure (if asked to do so) to be able to work in the
                                    // target project.

                                    // We always ensure that the workspaces:["ck-gen"] and "@local/ck-gen" dependency are here.
                                    if( !invalidPackageJson
                                        && YarnHelper.SetupTargetProjectPackageJson( monitor, projectJsonPath,
                                                                                     targetPackageJson,
                                                                                     out var testScriptCommand,
                                                                                     out var jestVersion,
                                                                                     out var tsJestVersion,
                                                                                     out var typesJestVersion ) )
                                    {
                                        // Before installing VSCode support, we must ensure that typescript is installed in the target
                                        // project, otherwise the TypeScript support won't be installed by the yarn sdks.
                                        // EnsureTestSupport => AutoInstallVSCodeSupport.
                                        if( (BinPathConfiguration.AutoInstallVSCodeSupport || BinPathConfiguration.EnsureTestSupport)
                                            && !YarnHelper.HasVSCodeSupport( monitor, targetProjectPath ) )
                                        {
                                            if( targetTypescriptVersion == null )
                                            {
                                                if( YarnHelper.DoRunYarn( monitor, targetProjectPath, "add --dev typescript", yarnPath.Value ) )
                                                {
                                                    // So the EnsureTestSupport won't reinstall it.
                                                    targetTypescriptVersion = "latest";
                                                }
                                            }
                                            YarnHelper.InstallVSCodeSupport( monitor, targetProjectPath, yarnPath.Value );
                                        }

                                        // If we must ensure test support, we consider that as soon as a "test" script is available
                                        // we are done: the goal is to support "yarn test", Jest is our default test framework but is
                                        // not required.
                                        if( BinPathConfiguration.EnsureTestSupport )
                                        {
                                            if( testScriptCommand != null )
                                            {
                                                monitor.Info( $"TypeScript test script command '{testScriptCommand}' already exists. Skipping EnsureJestTestSupport." );
                                            }
                                            else
                                            {
                                                success &= EnsureJestTestSupport( monitor,
                                                                                  targetProjectPath,
                                                                                  projectJsonPath,
                                                                                  targetTypescriptVersion,
                                                                                  yarnPath.Value,
                                                                                  jestVersion,
                                                                                  tsJestVersion,
                                                                                  typesJestVersion );
                                            }
                                        }

                                    }
                                }
                            }
                        }
                        else success = false;
                    }
                    if( cleanupPaths != null )
                    {
                        if( cleanupPaths.Count == 0 )
                        {
                            monitor.Info( "No previous file exist that have not been regenerated." );
                        }
                        else
                        {
                            using( monitor.OpenInfo( $"Deleting {cleanupPaths.Count} previous files." ) )
                            {
                                foreach( var p in cleanupPaths )
                                {
                                    monitor.Debug( $"Deleting '{p.AsSpan(ckGenFolder.Path.Length)}'." );
                                    try
                                    {
                                        if( File.Exists( p ) ) File.Delete( p );
                                    }
                                    catch( Exception ex )
                                    {
                                        monitor.Error( $"While deleting '{p}'. Ignoring.", ex );
                                    }
                                }
                            }
                        }
                    }
                }
                else success = false;
            }
            return success;
        }

        static bool EnsureJestTestSupport( IActivityMonitor monitor,
                                           NormalizedPath targetProjectPath,
                                           NormalizedPath projectJsonPath,
                                           string? targetTypescriptVersion,
                                           NormalizedPath yarnPath,
                                           string? jestVersion,
                                           string? tsJestVersion,
                                           string? typesJestVersion )
        {
            bool success = true;
            using( monitor.OpenInfo( $"Ensuring TypeScript test with Jest." ) )
            {
                string a = string.Empty, i = string.Empty;
                Add( ref a, ref i, "typescript", targetTypescriptVersion );
                Add( ref a, ref i, "jest", jestVersion );
                Add( ref a, ref i, "ts-jest", tsJestVersion );
                Add( ref a, ref i, "@types/jest", typesJestVersion );
                static void Add( ref string a, ref string i, string name, string? version )
                {
                    if( version == null )
                    {
                        if( i.Length == 0 ) i = name;
                        else i += ' ' + name;
                    }
                    else
                    {
                        a = $"{a}{(a.Length == 0 ? "" : ", ")}{name} ({version})";
                    }
                }

                if( a.Length > 0 ) monitor.Info( $"Already installed: {a}." );
                if( i.Length > 0 )
                {
                    success &= YarnHelper.DoRunYarn( monitor, targetProjectPath, $"add --dev {i}", yarnPath );
                }
                // Always setup jest even on error.
                YarnHelper.EnsureSampleJestTestInSrcFolder( monitor, targetProjectPath );
                YarnHelper.SetupJestConfigFile( monitor, targetProjectPath );

                var packageJsonObject = YarnHelper.LoadPackageJson( monitor, projectJsonPath, out var invalidPackageJson );
                // There is absolutely no reason here to not be able to read back the package.json.
                // Defensive programming here.
                if( !invalidPackageJson && packageJsonObject != null )
                {
                    bool modified = false;
                    var scripts = YarnHelper.EnsureJsonObject( monitor, packageJsonObject, "scripts", ref modified );
                    Throw.DebugAssert( scripts != null );
                    scripts.Add( "test", "jest" );
                    success &= YarnHelper.SavePackageJsonFile( monitor, projectJsonPath, packageJsonObject );
                }
            }

            return success;
        }
    }
}
