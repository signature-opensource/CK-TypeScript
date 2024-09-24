using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using DependencyKind = CK.TypeScript.CodeGen.DependencyKind;

namespace CK.Setup;

public sealed partial class TypeScriptIntegrationContext
{
    /// <summary>
    /// The default TypeScript version to consider when none can be found in the context.
    /// </summary>
    public const string DefaultTypeScriptVersion = "5.4.5";

    readonly TypeScriptBinPathAspectConfiguration _configuration;
    readonly NormalizedPath _ckGenFolder;
    readonly PackageJsonFile _targetPackageJson;
    readonly TSConfigJsonFile _tsConfigJson;
    readonly int _initialCKVersion;
    readonly bool _initialEmptyTargetPackage;
    readonly ImmutableDictionary<string,SVersionBound> _libVersionsConfig;
    readonly NormalizedPath _srcFolderPath;
    NormalizedPath _yarnPath;
    SVersion? _typeScriptSdkVersion;
    // Computed once by SettleTypeScriptDependency.
    PackageDependency? _settledTypeScriptDep;
    bool _shouldAlignYarnSdkVersion;
    // We must keep this as a member to be able to expose SettleTypeScriptDependency() on the BeforeEventArgs. 
    TypeScriptFileSaveStrategy? _saver;
    // Not null when AutoInstallJest is true. Can be substituted by BeforeEventArgs. 
    JestSetupHandler? _jestSetup;
    // Cached content of the target package.json file.
    string _lastInstalledTargetPackageJsonContent;

    TypeScriptIntegrationContext( TypeScriptBinPathAspectConfiguration configuration,
                                  PackageJsonFile targetPackageJson,
                                  TSConfigJsonFile tSConfigJson,
                                  ImmutableDictionary<string, SVersionBound> libVersionsConfig )
    {
        _initialCKVersion = targetPackageJson.CKVersion;
        _initialEmptyTargetPackage = targetPackageJson.IsEmpty;
        _lastInstalledTargetPackageJsonContent = targetPackageJson.WriteAsString();
        targetPackageJson.CKVersion = TypeScriptContext.CKTypeScriptCurrentVersion;
        _configuration = configuration;
        _ckGenFolder = configuration.TargetProjectPath.AppendPart( "ck-gen" );
        _targetPackageJson = targetPackageJson;
        _tsConfigJson = tSConfigJson;
        _libVersionsConfig = libVersionsConfig;
        _srcFolderPath = configuration.TargetProjectPath.AppendPart( "src" );
        if( configuration.AutoInstallJest )
        {
            _jestSetup = new JestSetupHandler( this );
        }
    }

    public TypeScriptBinPathAspectConfiguration Configuration => _configuration;

    /// <summary>
    /// Gets the ck-gen/ folder.
    /// </summary>
    public NormalizedPath CKGenFolder => _ckGenFolder;

    /// <summary>
    /// Gets the project target <see cref="PackageJsonFile"/>.
    /// </summary>
    public PackageJsonFile TargetPackageJson => _targetPackageJson;

    /// <summary>
    /// Gets the project target <see cref="TSConfigJsonFile"/>.
    /// </summary>
    public TSConfigJsonFile TSConfigJson => _tsConfigJson;

    /// <summary>
    /// Gets the /src folder.
    /// When starting from scratch, this direcory is guaranteed to exists after
    /// the <see cref="OnBeforeIntegration"/> step. Otherwise it should always exist.
    /// </summary>
    public NormalizedPath SrcFolderPath => _srcFolderPath;

    /// <summary>
    /// Gets the initial "ckVersion" from the <see cref="TargetPackageJson"/>.
    /// Used to handle migrations to the current <see cref="TypeScriptContext.CKTypeScriptCurrentVersion"/>:
    /// The <see cref="TargetPackageJson"/> <see cref="PackageJsonFile.CKVersion"/> is set to <see cref="TypeScriptContext.CKTypeScriptCurrentVersion"/>.
    /// </summary>
    public int InitialCKVersion => _initialCKVersion;

    /// <summary>
    /// Raised before the integration step when <see cref="TypeScriptBinPathAspectConfiguration.IntegrationMode"/> is
    /// not <see cref="CKGenIntegrationMode.None"/>.
    /// <para>
    /// When this event is raised we only know that Yarn is available but we don't know anything about the integration
    /// context (the TypeScript version is not known yet). The target project folder exists, the ck-gen/ folder has been generated but it may only contain the
    /// ck-gen/ folder.
    /// </para>
    /// This can be used to setup the target project before running (such as initializing from scratch an angular application).
    /// Any project structure can be setup (or altered) and <see cref="BaseEventArgs.RunYarn(string, Dictionary{string, string}?, NormalizedPath)"/>
    /// can be called.
    /// </summary>
    public event EventHandler<BeforeEventArgs>? OnBeforeIntegration;

    /// <summary>
    /// Raised after the integration step when <see cref="TypeScriptBinPathAspectConfiguration.IntegrationMode"/> is
    /// not <see cref="CKGenIntegrationMode.None"/>.
    /// </summary>
    public event EventHandler<AfterEventArgs>? OnAfterIntegration;

    internal static TypeScriptIntegrationContext? Create( IActivityMonitor monitor,
                                                          TypeScriptBinPathAspectConfiguration configuration,
                                                          ImmutableDictionary<string, SVersionBound> libVersionsConfig )
    {
        var targetProjectPath = configuration.TargetProjectPath;
        // The Json files are null if they cannot be read and empty for an unexisting one.
        Throw.DebugAssert( configuration.AspectConfiguration != null );
        var packageJson = PackageJsonFile.ReadFile( monitor, targetProjectPath.AppendPart( "package.json" ), configuration.AspectConfiguration.IgnoreVersionsBound );
        var tsConfigJson = TSConfigJsonFile.ReadFile( monitor, targetProjectPath.AppendPart( "tsConfig.json" ) );

        if( packageJson == null || tsConfigJson == null )
        {
            var what = packageJson == null ? "package.json" : null;
            if( tsConfigJson == null )
            {
                if( what != null ) what += " and ";
                what += "tsConfig.json";
            }
            monitor.Error( $"The target {what} cannot be read. This needs to be manually fixed." );
            return null;
        }
        return new TypeScriptIntegrationContext( configuration, packageJson, tsConfigJson, libVersionsConfig );
    }

    bool AddOrUpdateTargetProjectDependency( IActivityMonitor monitor, string name, SVersionBound? version, DependencyKind kind )
    {
        bool isConfigured = _libVersionsConfig.TryGetValue( name, out var configured );
        if( version == null )
        {
            if( !isConfigured )
            {
                monitor.Error( $"TypeScript library '{name}' version must be configured in TypeScript aspect LibraryVersions." );
                return false;
            }
            version = configured;
        }
        else
        {
            if( isConfigured && version != configured )
            {
                monitor.Info( $"TypeScript library '{name}' will use the configured version '{configured}'. Ignoring version '{version}'." );
                version = configured;
            }
        }
        var dependency = new PackageDependency( name, version.Value, kind );
        return _targetPackageJson.Dependencies.AddOrUpdate( monitor, dependency, cloneAddedDependency: false );
    }

    bool SettleTypeScriptVersion( IActivityMonitor monitor, [NotNullWhen(true)]out PackageDependency? typeScriptDep )
    {
        Throw.DebugAssert( _saver != null && _targetPackageJson != null );

        if( _settledTypeScriptDep != null )
        {
            typeScriptDep = _settledTypeScriptDep;
            return true;
        }
        // The currently installed typescript is in the target package.json if it exists (most common case).
        PackageDependency? fromTargetProject = _targetPackageJson.Dependencies.GetValueOrDefault( "typescript" );

        // Even if we don't know yet whether Yarn is installed, we lookup
        // the Yarn typescript sdk version to check TypeScript version homogeneity.
        // We must ensure that the Yarn typescript sdk is installed: if it's not, package resolution fails miserably.
        // We read the typeScriptSdkVersion here.
        _typeScriptSdkVersion = YarnHelper.GetYarnSdkTypeScriptVersion( monitor, _configuration.TargetProjectPath );

        // The code MAY have declared the typescript dependency.
        PackageDependency? fromCodeDeclared = _saver.GeneratedDependencies.GetValueOrDefault( "typescript" );

        // If we have a configured version for TypeScript, this is the one that should be used, no matter what.
        // But here, we miss a "Force = true" option in the LibraryVersions that will trigger a AddOrReplace.
        // For the moment, we only use AddOrUpdate: IgnoreVersionBound is honored, if the version cannot be merged,
        // then TypeScriptAspect.IgnoreVersionsBound should be set to true.
        if( _libVersionsConfig.TryGetValue( "typescript", out SVersionBound fromConfiguration ) )
        {
            typeScriptDep = new PackageDependency( "typescript", fromConfiguration, DependencyKind.DevDependency );
            if( !_targetPackageJson.Dependencies.AddOrUpdate( monitor, typeScriptDep, cloneAddedDependency: false ) )
            {
                return false;
            }
        }
        else
        {
            // No configuration. Choose the TypeScript version from these sources (descending order of priority):
            typeScriptDep = FindBestTypeScriptVersion( monitor,
                                                       fromTargetProject,
                                                       fromCodeDeclared,
                                                       _typeScriptSdkVersion,
                                                       _configuration.DefaultTypeScriptVersion );
            Throw.DebugAssert( fromTargetProject == null || fromTargetProject.Version == typeScriptDep.Version );
            if( fromTargetProject == null )
            {
                _targetPackageJson.Dependencies.AddOrReplace( typeScriptDep, false );
            }
        }

        // If code declared is not same as the final version, we must update the generated dependency for coherency sake
        // but mainly for NpmPackage mode: the typescript versions must be aligned.
        //
        // Should we AddOrUpdate or AddOrReplace here?
        // For the moment, continue to use AddOrReplace (honor TypeScriptAspect.IgnoreVersionsBound).
        //
        if( fromCodeDeclared != typeScriptDep
            && !_saver.GeneratedDependencies.AddOrUpdate( monitor, typeScriptDep, cloneAddedDependency: false ) )
        {
            return false;
        }
        // Handle Yarn TypeScript sdk.
        _shouldAlignYarnSdkVersion = _typeScriptSdkVersion != typeScriptDep.Version.Base;
        if( _shouldAlignYarnSdkVersion )
        {
            if( _typeScriptSdkVersion != null )
            {
                monitor.Warn( $"The Yarn TypeScript sdk version '{_typeScriptSdkVersion}' differs from the selected one '{typeScriptDep.Version.Base}'.{Environment.NewLine}" +
                                $"This can lead to (very) annoying issues such as import resolution failures. We'll try to fix this." );
            }
            else
            {
                monitor.Warn( $"The Yarn TypeScript sdk will be installed." );
            }
        }
        else
        {
            monitor.Trace( $"The Yarn TypeScript sdk is installed in same version ({_typeScriptSdkVersion})." );
        }

        _settledTypeScriptDep = typeScriptDep;
        return true;

        static PackageDependency FindBestTypeScriptVersion( IActivityMonitor monitor,
                                                            PackageDependency? targetProjectVersion,
                                                            PackageDependency? codeDeclaredVersion,
                                                            SVersion? typeScriptSdkVersion,
                                                            string? configurationDefaultTypeScriptVersion )
        {
            PackageDependency result;
            var source = "target project";
            if( targetProjectVersion is not null )
            {
                result = targetProjectVersion;
            }
            else if( codeDeclaredVersion is not null )
            {
                source = "code declared dependency";
                result = codeDeclaredVersion;
            }
            else
            {
                SVersionBound vResult;
                if( typeScriptSdkVersion != null )
                {
                    source = "Yarn TypeScript sdk";
                    vResult = new SVersionBound( typeScriptSdkVersion, SVersionLock.Lock, PackageQuality.Stable );
                }
                else if( configurationDefaultTypeScriptVersion != null )
                {
                    var parseResult = SVersionBound.NpmTryParse( configurationDefaultTypeScriptVersion );
                    if( parseResult.IsValid )
                    {
                        source = "BinPathConfiguration.DefaultTypeScriptVersion property";
                        vResult = parseResult.Result;
                    }
                    else
                    {
                        monitor.Warn( $"Unable to parse {source}: '{configurationDefaultTypeScriptVersion}'. Using '{DefaultTypeScriptVersion}'." );
                        FromCodeDefault( out vResult, out source );
                    }
                }
                else
                {
                    FromCodeDefault( out vResult, out source );
                }
                result = new PackageDependency( "typescript", vResult, DependencyKind.DevDependency );
            }
            monitor.Info( $"Considering TypeScript version '{result.Version}' from {source}." );
            return result;

            static void FromCodeDefault( out SVersionBound result, out string source )
            {
                source = "code default";
                var parseResult = SVersionBound.NpmTryParse( DefaultTypeScriptVersion );
                Throw.DebugAssert( "The version defined in code is necessarily valid.", parseResult.IsValid );
                result = parseResult.Result;
            }
        }
    }

    bool SaveTargetPackageJsonAndYarnInstall( IActivityMonitor monitor )
    {
        using var _ = monitor.OpenInfo( "Running Yarn install." );
        if( !SettleTypeScriptVersion( monitor, out var _ ) )
        {
            return false;
        }
        // We try to skip the run by default but if Yarn TypeScript sdk must be handled
        // or there are "latest" versions to install, this sill not be possible.
        bool canSkipRun = true;
        var finalCommand = new StringBuilder();
        if( _shouldAlignYarnSdkVersion )
        {
            canSkipRun = false;
            finalCommand.Append( "/C yarn add -D @yarnpkg/sdks && yarn sdks vscode" );
            _shouldAlignYarnSdkVersion = false;
        }
        else
        {
            finalCommand.Append( "/C yarn install" );
        }
        var manual = _targetPackageJson.Dependencies.RemoveLatestDependencies();
        if( manual.Count > 0 )
        {
            canSkipRun = false;
            var regDeps = manual.Where( d => d.DependencyKind is DependencyKind.Dependency or DependencyKind.PeerDependency );
            var devDeps = manual.Where( d => d.DependencyKind is DependencyKind.DevDependency );
            var peerDeps = manual.Where( d => d.DependencyKind is DependencyKind.PeerDependency );
            if( regDeps.Any() ) finalCommand.Append( $" && yarn add {regDeps.Select( d => d.Name ).Concatenate( " " )}" );
            if( devDeps.Any() ) finalCommand.Append( $" && yarn add --prefer-dev {devDeps.Select( d => d.Name ).Concatenate( " " )}" );
            if( peerDeps.Any() ) finalCommand.Append( $" && yarn add -P {peerDeps.Select( d => d.Name ).Concatenate( " " )}" );
        }
        if( canSkipRun )
        {
            var text = _targetPackageJson.WriteAsString();
            if( text == _lastInstalledTargetPackageJsonContent )
            {
                monitor.CloseGroup( "File package.json is up-to-date." );
                return true;
            }
            File.WriteAllText( _targetPackageJson.FilePath, text );
        }
        else
        {
            _targetPackageJson.Save();
        }
        if( !Run( monitor, finalCommand, _configuration.TargetProjectPath ) )
        {
            return false;
        }
        _targetPackageJson.Reload( monitor );
        _lastInstalledTargetPackageJsonContent = _targetPackageJson.WriteAsString();
        return true;

        static bool Run( IActivityMonitor monitor, StringBuilder finalCommand, NormalizedPath targetProjectPath )
        {
            var cmd = finalCommand.ToString();
            var displayCmd = cmd.Substring( 3 ).Replace( " && ", Environment.NewLine );
            using( monitor.OpenInfo( $"Running:{Environment.NewLine}{displayCmd}" ) )
            {
                return YarnHelper.RunProcess( monitor.ParallelLogger, "cmd.exe", cmd, targetProjectPath, null ) == 0;
            }
        }
    }

    internal bool Run( IActivityMonitor monitor, TypeScriptFileSaveStrategy saver )
    {
        // Obtains the yarn path or error.
        var yarnPath = YarnHelper.GetYarnInstallPath( monitor,
                                                      _configuration.TargetProjectPath,
                                                      _configuration.AutoInstallYarn );
        if( !yarnPath.HasValue ) return false;
        _yarnPath = yarnPath.Value;

        // Setup the target project dependencies according to the integration mode.
        if( _configuration.IntegrationMode == CKGenIntegrationMode.Inline )
        {
            MergeGeneratedDependencies( monitor, _targetPackageJson.Dependencies, saver.GeneratedDependencies.Values );
        }
        else
        {
            Throw.DebugAssert( _configuration.IntegrationMode == CKGenIntegrationMode.NpmPackage );
            MergeGeneratedDependencies( monitor, _targetPackageJson.Dependencies, saver.GeneratedDependencies.Values.Where( d => d.DependencyKind == DependencyKind.PeerDependency ) );
        }
        if( _configuration.AutoInstallJest )
        {
            _targetPackageJson.Scripts.TryAdd( "test", "jest" );
            AddOrUpdateTargetProjectDependency( monitor, "jest", SVersionBound.All, DependencyKind.DevDependency );
            AddOrUpdateTargetProjectDependency( monitor, "ts-jest", SVersionBound.All, DependencyKind.DevDependency );
            AddOrUpdateTargetProjectDependency( monitor, "@types/jest", SVersionBound.All, DependencyKind.DevDependency );
            AddOrUpdateTargetProjectDependency( monitor, "@types/node", SVersionBound.All, DependencyKind.DevDependency );
            AddOrUpdateTargetProjectDependency( monitor, "jest-environment-jsdom", SVersionBound.All, DependencyKind.DevDependency );
        }
        // Raising OnBeforeIntegration.
        _saver = saver;
        var hBefore = OnBeforeIntegration;
        if( hBefore != null
            && !RaiseEvent( monitor, hBefore, new BeforeEventArgs( monitor, this, _yarnPath ), nameof( OnBeforeIntegration ) ) )
        {
            return false;
        }
        // It is important to settle the TypeScript version here to ensure
        // that the _saver.GeneratedDependencies contains the right TypeScript version.
        // (NpmPackageIntegrate and TSPathInlineIntegrate would both have do this first.)
        if( !SettleTypeScriptVersion( monitor, out var typeScriptDep ) )
        {
            return false;
        }
        var success = _configuration.IntegrationMode switch
        {
            CKGenIntegrationMode.NpmPackage => NpmPackageIntegrate( monitor, saver, typeScriptDep ),
            CKGenIntegrationMode.Inline => TSPathInlineIntegrate( monitor, saver, typeScriptDep ),
            _ => Throw.NotSupportedException<bool>()
        };
        // Assumes that the /src folder exists.
        Directory.CreateDirectory( _srcFolderPath );
        // Raise the AfterIntegrationEvent
        var hAfter = OnAfterIntegration;
        if( hAfter != null
            && !RaiseEvent( monitor, hAfter, new AfterEventArgs( monitor, this, _yarnPath, success ), nameof( OnAfterIntegration ) ) )
        {
            success = false;
        }
        // Running Jest setup if AutoInstallJest is true.
        if( success && _jestSetup != null )
        {
            success = _jestSetup.DoRun( monitor );
        }
        // NpmPackageIntegrate and TSPathInlineIntegrate have done their job.
        // It is up to OnAfterIntegration to take care of saving any modification to package.json
        // or other resources.
        return success;

        bool RaiseEvent<T>( IActivityMonitor monitor, EventHandler<T> h, T e, string name )
        {
            var error = false;
            using( monitor.OpenInfo( $"Raising {name} event." ) )
            using( monitor.OnError( () => error = true ) )
            {
                try
                {
                    h( this, e );
                }
                catch( Exception ex )
                {
                    monitor.Error( ex );
                }
                if( error ) monitor.CloseGroup( "Failed." );
            }
            return !error;
        }

        static void MergeGeneratedDependencies( IActivityMonitor monitor, DependencyCollection c, IEnumerable<PackageDependency> dependencies )
        {
            int count = 0;
            var b = new StringBuilder();
            foreach( var d in dependencies )
            {
                if( c.TryGetValue( d.Name, out var exists ) )
                {
                    if( exists.Version != d.Version || exists.DependencyKind != d.DependencyKind )
                    {
                        if( count++ == 0 ) b.Append( ", " );
                        b.Append( exists ).Append( " to " ).Append( d );
                        c.AddOrReplace( d, false );
                    }
                }
                else
                {
                    if( count++ == 0 ) b.Append( ", " );
                    b.Append( " added ").Append( d );
                    c.AddOrReplace( d, false );
                }
            }
            if( count != 0 )
            {
                monitor.Info( $"Updated {count} dependencies in package.json from code generated dependencies:{Environment.NewLine}{b.ToString()}" );
            }
            else
            {
                monitor.Trace( "No dependencies in package.json to update from code generated dependencies." );
            }
        }
    }

    bool NpmPackageIntegrate( IActivityMonitor monitor,
                              TypeScriptFileSaveStrategy saver,
                              PackageDependency typeScriptDep )
    {
        // Generates "/ck-gen": "package.json", "tsconfig.json" and potentially "tsconfig-cjs.json" and "tsconfig-es6.json" files.
        // This may fail if there's an error in the dependencies declared by the code generator (in LibraryImport).
        if( !SaveCKGenBuildConfig( monitor,
                                   _ckGenFolder,
                                   saver.GeneratedDependencies,
                                   _configuration.ModuleSystem,
                                   _configuration.UseSrcFolder,
                                   _configuration.EnableTSProjectReferences ) )
        {
            return false;
        }

        // If the tsConfig is empty (it doesn't exist), let's create a default one: a tsConfig.json is 
        // required by our jest.config.js (and it should always exist).
        if( _tsConfigJson.EnsureDefault( monitor ) ) _tsConfigJson.Save();

        // The workspace dependency.
        PackageDependency ckGenDep = new PackageDependency( "@local/ck-gen", SVersionBound.None, DependencyKind.DevDependency );

        if( _initialEmptyTargetPackage )
        {
            _targetPackageJson.Name ??= _targetPackageJson.SafeName;
            _targetPackageJson.Private = true;
            _targetPackageJson.EnsureWorkspace( "ck-gen" );
            _targetPackageJson.Dependencies.AddOrUpdate( monitor, ckGenDep, cloneAddedDependency: false );
        }
        else
        {
            // We always ensure that the workspaces:["ck-gen"] and the "@local/ck-gen" dependency are here
            if( _targetPackageJson.Workspaces?.Contains( "*" ) is not true && _targetPackageJson.EnsureWorkspace( "ck-gen" ) )
            {
                monitor.Info( $"Added \"ck-gen\" workspace." );
            }
            if( !_targetPackageJson.Dependencies.TryGetValue( "@local/ck-gen", out var ck )
                || !ck.IsWorkspaceDependency
                || ck.DependencyKind != DependencyKind.DevDependency )
            {
                if( ck == null )
                {
                    _targetPackageJson.Dependencies.AddOrUpdate( monitor, ckGenDep, false );
                    monitor.Info( $"Added \"@local/ck-gen\" as a workspace development dependency." );
                }
                else
                {
                    ck.UnconditionalSetVersion( SVersionBound.None );
                    ck.UnconditionalSetDependencyKind( DependencyKind.DevDependency );
                    monitor.Info( $"Fixed \"@local/ck-gen\" as a workspace development dependency." );
                }
            }
        }
        // Tries to build the ck-gen/ folder even if an error occurred.
        return SaveTargetPackageJsonAndYarnInstall( monitor )
               & YarnHelper.DoRunYarn( monitor, _ckGenFolder, "run build", _yarnPath );
    }

    bool TSPathInlineIntegrate( IActivityMonitor monitor,
                                TypeScriptFileSaveStrategy saver,
                                PackageDependency typeScriptDep )
    {
        // If this fails, we don't care: this is purely informational.
        SaveCKGenBuildConfig( monitor,
                              _ckGenFolder,
                              saver.GeneratedDependencies,
                              _configuration.ModuleSystem,
                              _configuration.UseSrcFolder,
                              _configuration.EnableTSProjectReferences,
                              filePrefix: "CouldBe." );
        if( _initialEmptyTargetPackage )
        {
            _targetPackageJson.Name ??= _targetPackageJson.SafeName;
            _targetPackageJson.Private = true;
        }
        else
        {
            // Remove NpmPackage mode if any.
            if( _targetPackageJson.Dependencies.Remove( "@local/ck-gen" ) )
            {
                monitor.Info( "Removed '@local/ck-gen' package dependency (NpmPackage integration mode)." );
            }
            if( _targetPackageJson.Workspaces?.Remove( "ck-gen" ) is true )
            {
                monitor.Info( $"Removed 'ck-gen' Yarn workspace (NpmPackage integration mode)." );
            }
        }

        // If the tsConfig is empty (it doesn't exist), let's create a default one.
        bool shouldSaveTSConfig = _tsConfigJson.EnsureDefault( monitor );

        // Ensure that the compilerOptions:paths has entries:
        //   "@local/ck-gen": ["./ck-gen/src"]
        //   "@local/ck-gen/*": ["./ck-gen/src/*"]
        if( !_tsConfigJson.ResolvedBaseUrl.TryGetRelativePathTo( _configuration.TargetCKGenPath, out NormalizedPath mapping ) )
        {
            monitor.Error( $"Unable to compute relative path from tsConfig.json baseUrl '{_tsConfigJson.ResolvedBaseUrl}' to {_configuration.TargetCKGenPath}." );
            return false;
        }
        string to = "./" + mapping;
        // Only one | here! (We want to do both.)
        if( _tsConfigJson.CompileOptionsPathEnsureMapping( "@local/ck-gen", to )
            | _tsConfigJson.CompileOptionsPathEnsureMapping( "@local/ck-gen/*", to + "/*" ) )
        {
            monitor.Info( $"CompilerOption Paths mapped \"@local/ck-gen\" to \"{mapping}\"." );
            shouldSaveTSConfig = true;
        }
        if( shouldSaveTSConfig )
        {
            _tsConfigJson.Save();
        }
        // Installs everything.
        // We don't build anything (we don't knwon the build command at this level).
        return SaveTargetPackageJsonAndYarnInstall( monitor );
    }

    /// <summary>
    /// Generates "/ck-gen/package.json", "/ck-gen/tsconfig.json" and potentially "/ck-gen/tsconfig-cjs.json" and "/ck-gen/tsconfig-es6.json".
    /// </summary>
    internal static bool SaveCKGenBuildConfig( IActivityMonitor monitor,
                                               NormalizedPath ckGenFolder,
                                               DependencyCollection deps,
                                               TSModuleSystem moduleSystem,
                                               bool useSrcFolder,
                                               bool enableTSProjectReferences,
                                               string? filePrefix = null )
    {
        using var gLog = monitor.OpenInfo( $"Saving TypeScript and TypeScript configuration files..." );

        return GeneratePackageJson( monitor, ckGenFolder, moduleSystem, deps, filePrefix )
               && GenerateTSConfigJson( monitor, ckGenFolder, moduleSystem, useSrcFolder, enableTSProjectReferences, filePrefix );

        static bool GeneratePackageJson( IActivityMonitor monitor,
                                         NormalizedPath ckGenFolder,
                                         TSModuleSystem moduleSystem,
                                         DependencyCollection deps,
                                         string? filePrefix )
        {
            var packageJsonPath = Path.Combine( ckGenFolder, filePrefix + "package.json" );
            using( monitor.OpenTrace( $"Creating '{packageJsonPath}'." ) )
            {
                // The /ck-gen/package.json dependencies is bound to the generated one (into wich
                // typescript has been added).
                var p = PackageJsonFile.Create( packageJsonPath, deps );
                p.Name = "@local/ck-gen";

                if( moduleSystem is TSModuleSystem.ES6 or TSModuleSystem.ES6AndCJS or TSModuleSystem.CJSAndES6 )
                {
                    p.Module = "./dist/es6/index.js";
                }
                if( moduleSystem is TSModuleSystem.CJS or TSModuleSystem.ES6AndCJS or TSModuleSystem.CJSAndES6 )
                {
                    p.Main = "./dist/cjs/index.js";
                }
                var buildScript = "tsc -p tsconfig.json";
                if( moduleSystem == TSModuleSystem.ES6AndCJS )
                {
                    buildScript += " && tsc -p tsconfig-cjs.json";
                }
                else if( moduleSystem == TSModuleSystem.CJSAndES6 )
                {
                    buildScript += " && tsc -p tsconfig-es6.json";
                }
                p.Scripts.Add( "build", buildScript );
                p.Private = true;
                p.Save();
                return true;
            }
        }

        static bool GenerateTSConfigJson( IActivityMonitor monitor,
                                          NormalizedPath ckGenFolder,
                                          TSModuleSystem moduleSystem,
                                          bool useSrcFolder,
                                          bool enableTSProjectReferences,
                                          string? filePrefix )
        {
            var sb = new StringBuilder();
            var tsConfigFile = Path.Combine( ckGenFolder, filePrefix + "tsconfig.json" );
            using( monitor.OpenTrace( $"Creating '{tsConfigFile}'." ) )
            {
                string module, modulePath;
                string? otherModule = null, otherModulePath = null;
                string? unusedDist = null;
                var unusedConfigFiles = new List<string>();
                switch( moduleSystem )
                {
                    case TSModuleSystem.ES6:
                        module = "ES6";
                        modulePath = "es6";
                        unusedDist = "dist/cjs";
                        unusedConfigFiles.AddRangeArray( "tsconfig-cjs.json", "tsconfig-es6.json" );
                        break;
                    case TSModuleSystem.ES6AndCJS:
                        module = "ES6";
                        modulePath = "es6";
                        otherModule = "CommonJS";
                        otherModulePath = "cjs";
                        unusedConfigFiles.Add( "tsconfig-es6.json" );
                        break;
                    case TSModuleSystem.CJS:
                        module = "CommonJS";
                        modulePath = "cjs";
                        unusedDist = "dist/es6";
                        unusedConfigFiles.AddRangeArray( "tsconfig-cjs.json", "tsconfig-es6.json" );
                        break;
                    case TSModuleSystem.CJSAndES6:
                        module = "CommonJS";
                        modulePath = "cjs";
                        otherModule = "ES6";
                        otherModulePath = "es6";
                        unusedConfigFiles.Add( "tsconfig-cjs.json" );
                        break;
                    default: throw new CKException( "" );
                }
                DeleteUnused( monitor, ckGenFolder, unusedDist, unusedConfigFiles );

                // Allow this project to be "composite" (this is currently not supported by Jest).
                var tsBuildMode = "";
                if( enableTSProjectReferences )
                {
                    tsBuildMode = """
                                  ,
                                      "composite": true
                                  """;
                }
                string closer;
                if( useSrcFolder )
                {
                    closer = """
                        ,
                            "rootDir": "src",
                            "baseUrl": "./src"
                          },
                          "include": [ "src/**/*" ]
                        }

                        """;
                }
                else
                {
                    closer = $$"""

                          },
                          "exclude": [ "{{filePrefix}}tsconfig*.json", "{{filePrefix}}package.json" ]
                        }
                        """;

                }
                File.WriteAllText( tsConfigFile, $$"""
                        {
                          "compilerOptions": {
                            "strict": true,
                            "target": "es2022",
                            "moduleResolution": "node",
                            "lib": ["es2022", "dom"],
                            "module": "{{module}}",
                            "outDir": "./dist/{{modulePath}}",
                            "sourceMap": true,
                            "declaration": true,
                            "declarationMap": true,
                            "esModuleInterop": true,
                            "skipLibCheck": true,
                            "resolveJsonModule": true{{tsBuildMode}}{{closer}}
                        """ );
                if( otherModule != null )
                {
                    var tsConfigOtherFile = Path.Combine( ckGenFolder, $"{filePrefix}tsconfig-{otherModulePath}.json" );
                    monitor.Trace( $"Creating '{tsConfigOtherFile}'." );
                    File.WriteAllText( tsConfigOtherFile, $$"""
                                                {
                                                  "extends": "./tsconfig.json",
                                                  "compilerOptions": {
                                                    "module": "{{otherModule}}",
                                                    "outDir": "./dist/{{otherModulePath}}"
                                                  },
                                                }
                                                """ );
                }
            }
            return true;

            static void DeleteUnused( IActivityMonitor monitor, NormalizedPath outputPath, string? unusedDist, List<string> unusedConfigFiles )
            {
                if( unusedDist != null )
                {
                    var p = Path.Combine( outputPath, unusedDist );
                    if( Directory.Exists( p ) )
                    {
                        using( monitor.OpenInfo( $"Deleting no more used folder '{unusedDist}'." ) )
                        {
                            try
                            {
                                Directory.Delete( p, true );
                            }
                            catch( Exception ex )
                            {
                                monitor.Warn( $"Unable to delete directory '{p}'. Ignoring.", ex );
                            }
                        }
                    }
                }
                foreach( var f in unusedConfigFiles )
                {
                    var p = Path.Combine( outputPath, f );
                    if( File.Exists( p ) )
                    {
                        using( monitor.OpenInfo( $"Deleting useless file '{f}'." ) )
                        {
                            try
                            {
                                File.Delete( p );
                            }
                            catch( Exception ex )
                            {
                                monitor.Warn( $"Unable to delete file '{p}'. Ignoring.", ex );
                            }
                        }
                    }
                }
            }
        }
    }


    internal static void WarnDiffTypeScriptSdkVersion( IActivityMonitor monitor, SVersion typeScriptSdkVersion, SVersion targetTypeScriptVersion )
    {
        if( typeScriptSdkVersion != targetTypeScriptVersion )
        {
            monitor.Warn( $"The TypeScript version used by the Yarn sdk '{typeScriptSdkVersion}' differs from the selected one '{targetTypeScriptVersion}'.{Environment.NewLine}" +
                          $"This can lead to (very) annoying issues such as import resolution failures." );
        }
    }

}
