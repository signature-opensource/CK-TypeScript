using CK.Core;
using CK.Setup;
using CK.Transform.Core;
using CK.TypeScript.CodeGen;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.RegularExpressions;
using CK.TypeScript.Transform;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Implements support for Angular projects.
/// </summary>
public partial class AngularCodeGeneratorImpl : ITSCodeGeneratorFactory
{
    const string _defaultAngularCliVersion = "^20";
    const string _conflictFolderName = "_ckConflict_";

    ITSCodeGenerator? ITSCodeGeneratorFactory.CreateTypeScriptGenerator( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
    {
        if( initializer.BinPathConfiguration.IntegrationMode != CKGenIntegrationMode.Inline )
        {
            monitor.Error( $"Angular application requires Inline IntegrationMode. '{initializer.BinPathConfiguration}' mode is not supported." );
            return null;
        }
        var codeGen = new AngularCodeGen();
        initializer.RootMemory.Add( typeof( AngularCodeGen ), codeGen );
        return codeGen;
    }

    internal sealed class AngularCodeGen : ITSCodeGenerator, IAngularContext
    {
        [AllowNull] ComponentManager _components;
        [AllowNull] LibraryImport _angularCore;
        [AllowNull] LibraryImport _angularRouter;
        [AllowNull] TypeScriptFile _ckGenAppModule;
        [AllowNull] ITSCodePart _importModulePart;
        [AllowNull] ITSCodePart _exportModulePart;
        [AllowNull] ITSCodePart _providerPart;

        public ITSFileImportSection CKGenAppModuleImports => _ckGenAppModule.Imports;

        public ComponentManager ComponentManager => _components;

        LibraryImport IAngularContext.AngularCoreLibrary => _angularCore;

        LibraryImport IAngularContext.AngularRouterLibrary => _angularRouter;

        ITSCodePart IAngularContext.ProviderPart => _providerPart;

        /// <summary>
        /// Called by NgProviderAttributeImpl.
        /// </summary>
        internal void AddNgProvider( string providerCode, string sourceName )
        {
            _providerPart.Append( "CKGenAppModule.s( " ).Append( providerCode ).Append( ", " ).AppendSourceString( sourceName ).Append( " )," ).NewLine();
        }

        /// <summary>
        /// Called by NgModuleAttributeImpl.
        /// </summary>
        internal bool RegisterModule( IActivityMonitor monitor, NgModuleAttributeImpl module, ITSDeclaredFileType tsType )
        {
            _ckGenAppModule.Imports.Import( tsType );
            if( !_importModulePart.IsEmpty )
            {
                _importModulePart.Append( ", " );
                _exportModulePart.Append( ", " );
            }
            _importModulePart.Append( module.ModuleName );
            _exportModulePart.Append( module.ModuleName );
            return true;
        }

        bool ITSCodeGenerator.StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
        {
            if( !EnsureAngularIsInstalled( monitor, context ) )
            {
                return false;
            }
            Throw.DebugAssert( "Inline mode => IntegrationContext.", context.IntegrationContext != null );

            _angularCore = context.Root.LibraryManager.RegisterLibrary( monitor, "@angular/core", DependencyKind.Dependency );
            _angularRouter = context.Root.LibraryManager.RegisterLibrary( monitor, "@angular/router", DependencyKind.Dependency );

            _ckGenAppModule = context.Root.Root.FindOrCreateTypeScriptFile( "CK/Angular/CKGenAppModule.ts" );
            _ckGenAppModule.Imports.ImportFromLibrary( _angularCore, "NgModule, Provider, EnvironmentProviders" );
            _ckGenAppModule.Body.Append( """
                
                export type SourcedProvider = (EnvironmentProviders | Provider) & {source: string};

                /**
                 * Array-like of Provider or EnvironmentProviders that supports {@link exclude}
                 * to remove some of them: they can be manually reinjected if nominal configuration
                 * must be changed.
                 */
                export class SourcedProviders extends Array<SourcedProvider> {
                    /**
                     * Excludes all the providers issued by the given source.
                     * At least one such provider must exist otherwise this throws.
                     */
                    exclude( sourceName: string ): SourcedProviders
                    {
                        let idx = this.findIndex( s => s.source === sourceName );
                        if( idx < 0 ) throw new Error( `No provider from source '${sourceName}' found.` );
                        do
                        {
                            this.splice( idx, 1 );
                        }
                        while( (idx = this.findIndex( s => s.source === sourceName )) >= 0 );
                        return this;
                    }
                    
                    static createFrom(o: SourcedProvider[]) {
                        const sp = new SourcedProviders();
                        sp.push(...o);
                        return sp;
                    }
                }

                @NgModule({
                    imports: [

                """ )
                .InsertPart( out _importModulePart )
                .Append( """

                    ],
                    exports: [

                """ )
                .InsertPart( out _exportModulePart )
                .Append( """

                    ] })
                export class CKGenAppModule {
                    private static s( p: EnvironmentProviders | Provider, source: string ) : SourcedProvider
                    {
                        const s = <SourcedProvider>p;
                        s.source = source; 
                        return s;
                    }

                    static Providers : SourcedProviders = SourcedProviders.createFrom( [

                """ )
                .InsertPart( out _providerPart )
                .Append( """

                    ] );
                }
                """ );

            _components = new ComponentManager( context, _angularCore );
            context.IntegrationContext.OnBeforeIntegration += OnBeforeIntegration;
            context.IntegrationContext.OnAfterIntegration += OnAfterIntegration;
            return true;

            static bool EnsureAngularIsInstalled( IActivityMonitor monitor, TypeScriptContext context )
            {
                Throw.DebugAssert( context.IntegrationContext != null );
                if( !ValidateConfiguration( monitor, context.IntegrationContext.Configuration ) )
                {
                    return false;
                }
                var angularJsonPath = context.BinPathConfiguration.TargetProjectPath.AppendPart( "angular.json" );
                if( File.Exists( angularJsonPath ) )
                {
                    monitor.Info( "Found existing 'angular.json' file." );

                    // TODO: Update angular deps if needed

                    // Read package.json to retrieve @angular/core version.
                    // Check if it matches _defaultAngularCliVersion
                    // Maybe only upgrade if current installed version is inferior to _defaultAngularCliVersion
                    // do apply _defaultAngularCliVersion on @angular/* packages.
                    // yarn ng update ng-zorro-antd
                    // yarn ng update @angular/cli@^20 @angular/core@^20 ng-zorro-antd@^20 --allow-dirty --migrate-only
                    // Note: @angular/cli requires nodejs >= v22.12 (note: v22.18 works with PnP, versions below don't)

                    // choco install nodejs version="22.18.0"

                    return true;
                }
                return CreateAngularApp( monitor, context );

                static bool ValidateConfiguration( IActivityMonitor monitor, TypeScriptBinPathAspectConfiguration configuration )
                {
                    if( configuration.IntegrationMode != CKGenIntegrationMode.Inline )
                    {
                        monitor.Error( $"Angular application must always use TypeScriptBinPathAspectConfiguration.IntegrationMode = Inline." );
                        return false;
                    }
                    if( !configuration.AutoInstallJest )
                    {
                        monitor.Error( $"Angular application must always have test support: TypeScriptBinPathAspectConfiguration.AutoInstallJest must be set to true." );
                        return false;
                    }
                    return true;
                }

                static bool CreateAngularApp( IActivityMonitor monitor, TypeScriptContext context )
                {
                    Throw.DebugAssert( context.IntegrationContext != null );
                    using( monitor.OpenInfo( "No 'angular.json' found, creating a default Angular application." ) )
                    {
                        // This doesn't work: the npx.cmd looks for a npx-cli.js in the nodemodules/ of the workingDirectory instead
                        // of the nodemodules/ of the nodes/ or npm/ folder (the %~dp0 variable).
                        //
                        // e.RunProcess( "npx.cmd",
                        //               $"--package @angular/cli@18 ng new {e.TargetProjectPath.LastPart} --style=less --package-manager=yarn --interactive=false --standalone=true --minimal=true --skip-install=true --skip-git=true",
                        //               workingDirectory: e.TargetProjectPath.RemoveLastPart() );
                        //

                        NormalizedPath targetProjectPath = context.BinPathConfiguration.TargetProjectPath;
                        NormalizedPath tempFolderPath = targetProjectPath.AppendPart( "_ckNew_" );
                        NormalizedPath newFolderPath = tempFolderPath.AppendPart( targetProjectPath.LastPart.Replace( ' ', '_' ) );
                        NormalizedPath tempPackageJsonPath = tempFolderPath.AppendPart( "package.json" );
                        PackageJsonFile targetPackageJson = context.IntegrationContext.TargetPackageJson;

                        // We setup a package.json with angular/cli and calls 'ng new' on a sub folder and then
                        // we lift the folder's content.
                        //
                        // This is "safe": when lifting, if a file or a directory exists in the target project, the move
                        // will throw. This guaranties that no existing files will be lost: in practice, the target project
                        // folder must be empty (any existing target package.json is deleted before lifting).
                        //
                        // Instead of updating the target package.json with the angular one, we do it in the reverse way:
                        // the /_new_/xxx/package.json is updated with the content of the in memory (with its configured dependency),
                        // saved and lifted. Then the target package.json is reloaded from its "angular aware" file.

                        if( Directory.Exists( tempFolderPath ) )
                        {
                            monitor.Warn( $"An existing '_ckNew_' folder exists. Deleting it." );
                            TypeScriptContext.DeleteFolder( monitor, newFolderPath, recursive: true );
                        }

                        return CreateNewAngular( monitor, context, tempFolderPath, newFolderPath, tempPackageJsonPath )
                               && UpdateNewPackageJson( monitor, newFolderPath, targetPackageJson, out IList<PackageDependency> savedLatestDependencies, out var newPackageJson )
                               // We remove the "src/styles.less" file because we have already created it. 
                               && DeleteStylesLess( monitor, newFolderPath )
                               && TransformAppComponent( monitor, newFolderPath.Combine( "src/app/app.ts" ) )
                               && TransformAppComponentSpec( monitor, newFolderPath.Combine( "src/app/app.spec.ts" ) )
                               && TransformAppComponentConfig( monitor, newFolderPath.Combine( "src/app/app.config.ts" ) )
                               && TransformAppComponentRoutes( monitor, newFolderPath.Combine( "src/app/app.routes.ts" ) )
                               && CleanupAppComponentHtml( monitor, newFolderPath )
                               && SetupAngularJsonFile( monitor, newFolderPath )
                               && LiftContent( monitor, targetProjectPath, newFolderPath )
                               && ReloadTargetTSConfigAndPackageJson( monitor, context.IntegrationContext.TSConfigJson, targetPackageJson, savedLatestDependencies )
                               && DeleteNewFolder( monitor, tempFolderPath, newFolderPath, tempPackageJsonPath );
                    }

                    static bool CreateNewAngular( IActivityMonitor monitor,
                                                  TypeScriptContext context,
                                                  NormalizedPath tempFolderPath,
                                                  NormalizedPath newFolderPath,
                                                  NormalizedPath tempPackageJsonPath )
                    {
                        Throw.DebugAssert( context.IntegrationContext != null );
                        using( monitor.OpenInfo( $"Initialize a new Angular '{newFolderPath.LastPart}' project in the folder '{newFolderPath}'." ) )
                        {
                            Directory.CreateDirectory( tempFolderPath );
                            if( !context.IntegrationContext.ConfiguredLibraries.TryGetValue( "@angular/cli", out var angularCliVersion ) )
                            {
                                var parseResult = SVersionBound.NpmTryParse( _defaultAngularCliVersion );
                                Throw.DebugAssert( "The version defined in code is necessarily valid and not All.", parseResult.IsValid && parseResult.Result != SVersionBound.All );
                                angularCliVersion = parseResult.Result;
                                monitor.Info( $"Using @angular/cli default version '{_defaultAngularCliVersion}'." );
                            }
                            else
                            {
                                monitor.Info( $"Using @angular/cli version '{angularCliVersion}' from configured LibraryVersions." );
                            }
                            monitor.Info( $"Creating a temporary package.json." );
                            File.WriteAllText( tempPackageJsonPath, "{}" );
                            // Required by Yarn, otherwise the _new_ project must be a workspace in the parent project.
                            File.WriteAllText( tempFolderPath.AppendPart( "yarn.lock" ), "" );
                            if( !context.IntegrationContext.RunYarn( monitor,
                                            $"add @angular/cli@{angularCliVersion.ToNpmString()}",
                                            workingDirectory: tempFolderPath ) )
                            {
                                return false;
                            }
                            if( !context.IntegrationContext.RunYarn(
                                            monitor,
                                            $"ng new {newFolderPath.LastPart} --style=less --package-manager=yarn --interactive=false --standalone=true --skip-install=true --skip-git=true",
                                            workingDirectory: tempFolderPath ) )
                            {
                                return false;
                            }
                            return true;
                        }
                    }

                    static bool UpdateNewPackageJson( IActivityMonitor monitor,
                                                      NormalizedPath newFolderPath,
                                                      PackageJsonFile targetPackageJson,
                                                      out IList<PackageDependency> savedLatestDependencies,
                                                      [NotNullWhen( true )] out PackageJsonFile? newPackageJson )
                    {
                        savedLatestDependencies = ImmutableArray<PackageDependency>.Empty;
                        newPackageJson = PackageJsonFile.ReadFile( monitor,
                                                                   newFolderPath.AppendPart( "package.json" ),
                                                                   "@angular/cli created package.json",
                                                                   targetPackageJson.Dependencies.IgnoreVersionsBound,
                                                                   mustExist: true );
                        if( newPackageJson == null )
                        {
                            return false;
                        }

                        var toRemove = newPackageJson.Dependencies.Keys.Where( name => name.Contains( "jasmine" ) || name.Contains( "karma" ) ).ToList();
                        monitor.Info( $"Removing karma and jasmine packages: {toRemove.Concatenate()}." );
                        foreach( var name in toRemove ) newPackageJson.Dependencies.Remove( name );

                        // Running tests must be done via Jest, not anymore with "ng test" (that uses karma). 
                        newPackageJson.CKVersion = targetPackageJson.CKVersion;
                        newPackageJson.Scripts["test"] = "jest";

                        savedLatestDependencies = targetPackageJson.Dependencies.RemoveLatestDependencies();
                        if( !newPackageJson.Dependencies.AddOrUpdate( monitor, targetPackageJson.Dependencies.Values, cloneDependencies: false ) )
                        {
                            return false;
                        }
                        newPackageJson.Save();
                        return true;
                    }

                    static bool DeleteStylesLess( IActivityMonitor monitor, NormalizedPath newFolderPath )
                    {
                        NormalizedPath filePath = newFolderPath.Combine( "src/styles.less" );
                        if( File.Exists( filePath ) )
                        {
                            File.Delete( filePath );
                        }
                        return true;
                    }

                    static bool TransformAppComponent( IActivityMonitor monitor, NormalizedPath appFilePath )
                    {
                        var app = File.ReadAllText( appFilePath );
                        using( monitor.OpenInfo( "Transforming file 'src/app/component.ts'." ) )
                        {
                            var importLine = "import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';";
                            bool success = AddCKGenAppModuleInImports( monitor, ref app );
                            return AddImportAndConclude( monitor,
                                                         appFilePath,
                                                         success,
                                                         ref app,
                                                         importLine );
                        }

                        static bool AddCKGenAppModuleInImports( IActivityMonitor monitor, ref string app )
                        {
                            int idx = app.IndexOf( "imports: [" );
                            if( idx > 0 )
                            {
                                idx = app.IndexOf( "RouterOutlet", idx );
                                if( idx > 0 )
                                {
                                    Throw.DebugAssert( "RouterOutlet".Length == 12 );
                                    int idxEnd = idx + 12;
                                    while( app[idxEnd] != ',' && app[idxEnd] != ']' && idxEnd < app.Length ) ++idxEnd;
                                    if( idxEnd < app.Length )
                                    {
                                        if( app[idxEnd] == ',' )
                                        {
                                            app = app.Insert( idxEnd, " CKGenAppModule," );
                                        }
                                        else
                                        {
                                            Throw.DebugAssert( app[idxEnd] == ']' );
                                            app = app.Insert( idx + 12, ", CKGenAppModule" );
                                        }
                                        monitor.Info( "Added 'CKGenAppModule' in @Component imports." );
                                        return true;
                                    }
                                }
                            }
                            monitor.Warn( "Unable to find an 'imports: [ ... RouterOutlet ...]' substring." );
                            return false;
                        }
                    }

                    static bool TransformAppComponentSpec( IActivityMonitor monitor, NormalizedPath appFilePath )
                    {
                        var _ = monitor.OpenInfo( "Adding appConfig providers to TestBed in 'src/app/app.spec.ts'." );
                        var host = new TransformerHost( new TypeScriptLanguage() );
                        var f = host.TryParseFunction( monitor, """"
                            create <ts> transformer
                            begin
                                ensure import { appConfig } from './app.config';
                                in after "await TestBed.configureTestingModule"
                                    in first {^{}}
                                        insert """
                                                     // Added by CK.TS.AngularEngine: DI is fully configured and available in tests.
                                                     providers: appConfig.providers,

                                               """
                                            before "imports:";
                            end
                            """" );
                        if( f != null )
                        {
                            var app = File.ReadAllText( appFilePath );
                            var code = host.Transform( monitor, app, f );
                            if( code != null )
                            {
                                File.WriteAllText( appFilePath, code.ToString() );
                            }
                        }
                        return true;
                    }

                    static bool TransformAppComponentConfig( IActivityMonitor monitor, NormalizedPath configFilePath )
                    {
                        var app = File.ReadAllText( configFilePath );
                        using( monitor.OpenInfo( "Transforming file 'src/app/app.config.ts'." ) )
                        {
                            const string importLine = "import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';";
                            bool success = true;
                            int idx = app.IndexOf( "provideRouter(routes)" );
                            if( idx < 0 )
                            {
                                monitor.Warn( "Unable to find the 'provideRouter(routes)' substring." );
                                success = false;
                            }
                            else
                            {
                                Throw.DebugAssert( "provideRouter(routes)".Length == 21 );
                                app = app.Insert( idx + 21, ", ...CKGenAppModule.Providers," );
                                monitor.Info( "Added '...CKGenAppModule.Providers' in providers." );
                            }
                            return AddImportAndConclude( monitor, configFilePath, success, ref app, importLine );
                        }
                    }

                    static bool TransformAppComponentRoutes( IActivityMonitor monitor, NormalizedPath routesFilePath )
                    {
                        var app = File.ReadAllText( routesFilePath );
                        using( monitor.OpenInfo( "Transforming file 'src/app/app.routes.ts'." ) )
                        {
                            bool success = true;
                            int idx = app.LastIndexOf( "];" );
                            if( idx < 0 )
                            {
                                monitor.Warn( "Unable to find a closing '];'." );
                                success = false;
                            }
                            else
                            {
                                if( !NeedComma( app, idx, out var needComma ) )
                                {
                                    monitor.Warn( "Unable to find te start of the array." );
                                    success = false;
                                }
                                else
                                {
                                    app = app.Insert( idx, $"{Environment.NewLine}{(needComma ? ',' : ' ')}...CKGenRoutes{Environment.NewLine}" );
                                    monitor.Info( "Added '...CKGenRoutes' in routes." );
                                }
                            }
                            return AddImportAndConclude( monitor, routesFilePath, success, ref app, "import CKGenRoutes from '@local/ck-gen/CK/Angular/routes';" );
                        }

                        static bool NeedComma( string app, int idx, out bool needComma )
                        {
                            int idxC = idx - 1;
                            while( idxC > 0 )
                            {
                                if( app[idxC] == '[' )
                                {
                                    needComma = false;
                                    return true;
                                }
                                if( !char.IsWhiteSpace( app[idxC] ) )
                                {
                                    needComma = true;
                                    return true;
                                }
                                --idxC;
                            }
                            needComma = false;
                            return false;
                        }
                    }

                    static bool CleanupAppComponentHtml( IActivityMonitor monitor, NormalizedPath newFolderPath )
                    {
                        NormalizedPath filePath = newFolderPath.Combine( "src/app/app.html" );
                        const string defaultApp = """
                        <h1>Hello, {{ title() }}</h1>

                        <router-outlet />

                        """;
                        File.WriteAllText( filePath, defaultApp );
                        monitor.Trace( $"""
                                   File 'app.html' is:
                                   {defaultApp}
                                   """ );
                        return true;
                    }

                    static bool SetupAngularJsonFile( IActivityMonitor monitor, NormalizedPath newFolderPath )
                    {
                        var path = newFolderPath.AppendPart( "angular.json" );
                        if( !File.Exists( path ) )
                        {
                            monitor.Warn( $"Expecting file '{path}'. Skipping \"tests\" section removal and \"budgets\" configuration." );
                            return true;
                        }
                        using var _ = monitor.OpenInfo( "Processing file '{path}': removing \"tests\" section and configuring \"budgets.maximumError\" to \"1GB\"." );
                        try
                        {
                            bool testRemoved = false;
                            bool budgetsSet = false;
                            var angularJson = File.ReadAllText( path );
                            var root = JsonNode.Parse( angularJson );
                            if( root != null )
                            {
                                var architect = root["projects"]?[0]?["architect"]?.AsObject();
                                if( architect != null )
                                {
                                    var budgets = architect["build"]?["configurations"]?["production"]?["budgets"]?.AsArray();
                                    if( budgets != null )
                                    {
                                        foreach( var b in budgets )
                                        {
                                            var o = b?.AsObject();
                                            if( o != null )
                                            {
                                                if( o["maximumError"] != null )
                                                {
                                                    budgetsSet = true;
                                                    o["maximumError"] = "1GB";
                                                }
                                            }
                                        }
                                    }
                                    if( !budgetsSet )
                                    {
                                        monitor.Warn( "Unable to find any \"budgets.maximumError\" to set them to \"1GB\"." );
                                    }
                                    testRemoved = architect.Remove( "test" );
                                    if( !testRemoved )
                                    {
                                        monitor.Warn( "Unable to find \"architect.test\" section to remove." );
                                    }
                                }
                                if( testRemoved || budgetsSet )
                                {
                                    using( var f = File.Create( path ) )
                                    using( var w = new Utf8JsonWriter( f, new JsonWriterOptions { Indented = true } ) )
                                    {
                                        root.WriteTo( w );
                                    }
                                }
                            }
                        }
                        catch( Exception ex )
                        {
                            monitor.Warn( $"While processing file '{path}'.", ex );
                        }
                        return true;
                    }

                    static bool LiftContent( IActivityMonitor monitor,
                                             NormalizedPath targetProjectPath,
                                             NormalizedPath newFolderPath )
                    {
                        var conflictFolderPath = targetProjectPath.AppendPart( _conflictFolderName );

                        using( monitor.OpenInfo( "Lifts the new project content." ) )
                        {
                            var hasConflict = false;
                            if( Directory.Exists( conflictFolderPath ) )
                            {
                                monitor.Warn( $"Deleting previous '{_conflictFolderName}' folder." );
                                if( !TypeScriptContext.DeleteFolder( monitor, conflictFolderPath, recursive: true ) )
                                {
                                    return false;
                                }
                            }
                            int idxRemove = targetProjectPath.Path.Length;
                            int lenRemove = newFolderPath.Path.Length - idxRemove;
                            string? currentTarget = null;
                            try
                            {
                                // This is idempotent (as long as the comment is not removed in the lifted file).
                                MoveGitIgnore( newFolderPath, targetProjectPath );

                                // Process the src directory first and file by files instead of using Directory/file moves.
                                var newSrcPath = newFolderPath.AppendPart( "src" );
                                // Ensure that the directories exist.
                                foreach( var srcDir in Directory.EnumerateDirectories( newSrcPath, "*", SearchOption.AllDirectories ) )
                                {
                                    currentTarget = srcDir.Remove( idxRemove, lenRemove );
                                    if( !Directory.Exists( currentTarget ) )
                                    {
                                        Directory.CreateDirectory( currentTarget );
                                    }
                                }
                                // Then for each file, either move it to the target /src or /_ckConflict_ directory.
                                foreach( var fName in Directory.EnumerateFiles( newSrcPath, "*", SearchOption.AllDirectories ) )
                                {
                                    currentTarget = fName.Remove( idxRemove, lenRemove );
                                    if( File.Exists( currentTarget ) )
                                    {
                                        MoveToConflicts( monitor, ref hasConflict, targetProjectPath.Path.Length, currentTarget, conflictFolderPath );
                                    }
                                    else
                                    {
                                        File.Move( fName, currentTarget );
                                    }
                                }
                                // Deletes the now empty src folder... but it is not really empty because of empty folders (/src/app).
                                // Use recursive cleanup here.
                                TypeScriptContext.DeleteFolder( monitor, newSrcPath, recursive: true );

                                // Consider the root _new_/XXX/ level by using Directory.Move that handles
                                // folders and files.
                                // On conflict, the /_ckConflict_ will contain the whole sub folder.
                                foreach( var entry in Directory.EnumerateFileSystemEntries( newFolderPath ) )
                                {
                                    currentTarget = entry.Remove( idxRemove, lenRemove );
                                    if( Path.Exists( currentTarget ) )
                                    {
                                        // We have handled /src specifically above.
                                        if( !currentTarget.EndsWith( Path.DirectorySeparatorChar + "src" ) )
                                        {
                                            MoveToConflicts( monitor, ref hasConflict, targetProjectPath.Path.Length, currentTarget, conflictFolderPath );
                                        }
                                    }
                                    else
                                    {
                                        Directory.Move( entry, currentTarget );
                                    }
                                }
                            }
                            catch( Exception ex )
                            {
                                monitor.Error( $"While updating '{currentTarget}'.", ex );
                                return false;
                            }
                            return true;
                        }

                        static bool MoveToConflicts( IActivityMonitor monitor,
                                                     ref bool hasConflict,
                                                     int targetProjectPathLength,
                                                     string currentTarget,
                                                     NormalizedPath conflictFolderPath )
                        {
                            if( !hasConflict )
                            {
                                hasConflict = true;
                                monitor.Warn( $"Conflicts found at least on '{currentTarget.AsSpan( targetProjectPathLength )}'. See '{_conflictFolderName}' folder." );
                                Directory.CreateDirectory( conflictFolderPath );
                                File.WriteAllText( conflictFolderPath + "/.gitignore", "*" );
                            }
                            try
                            {
                                Throw.DebugAssert( _conflictFolderName == "_ckConflict_" );
                                Directory.Move( currentTarget, currentTarget.Insert( targetProjectPathLength, "/_ckConflict_" ) );
                            }
                            catch( Exception ex )
                            {
                                monitor.Error( $"While moving '{currentTarget.AsSpan( targetProjectPathLength )}' to '{_conflictFolderName}' folder.", ex );
                                return false;
                            }
                            return true;
                        }

                        static void MoveGitIgnore( NormalizedPath newFolder, NormalizedPath targetProjectPath )
                        {
                            var source = newFolder.AppendPart( ".gitignore" );
                            var target = targetProjectPath.AppendPart( ".gitignore" );
                            if( File.Exists( target ) )
                            {
                                var content = File.ReadAllText( target );
                                if( !content.Contains( "# From Angular initialization:" ) )
                                {
                                    content += Environment.NewLine + "# From Angular initialization:" + Environment.NewLine;
                                    content += File.ReadAllText( source );
                                    File.WriteAllText( target, content );
                                    File.Delete( source );
                                }
                            }
                            else
                            {
                                Directory.Move( source, target );
                            }
                        }
                    }

                    static bool ReloadTargetTSConfigAndPackageJson( IActivityMonitor monitor,
                                                                    TSConfigJsonFile tSConfigJson,
                                                                    PackageJsonFile targetPackageJson,
                                                                    IList<PackageDependency> savedLatestDependencies )
                    {
                        monitor.Info( "Reloads the now Angular aware target package.json and tsconfig.json files." );
                        targetPackageJson.Reload( monitor );
                        if( savedLatestDependencies.Count > 0 )
                        {
                            if( !targetPackageJson.Dependencies.AddOrUpdate( monitor, savedLatestDependencies, cloneDependencies: false ) )
                            {
                                return false;
                            }
                        }
                        tSConfigJson.Reload( monitor );
                        return true;
                    }

                    static bool DeleteNewFolder( IActivityMonitor monitor, NormalizedPath tempFolderPath, NormalizedPath newFolderPath, NormalizedPath tempPackageJsonPath )
                    {
                        // This is deliberately explicit about what SHOULD be left in the folders: we don't use a recursive blind deletion.
                        // If something change here, we need to know and fully handle the issue.
                        monitor.Info( "Deleting the 'package.json', 'yarn.lock', '.pnp.cjs' and '.pnp.loader.mjs' files and '.yarn' folder that has been used to install the @angular/cli." );
                        File.Delete( tempPackageJsonPath );
                        File.Delete( tempFolderPath + "/yarn.lock" );
                        File.Delete( tempFolderPath + "/.pnp.cjs" );
                        File.Delete( tempFolderPath + "/.pnp.loader.mjs" );
                        TypeScriptContext.DeleteFolder( monitor, tempFolderPath + "/.yarn", recursive: true );
                        monitor.Info( "Removing the now empty folder." );
                        return TypeScriptContext.DeleteFolder( monitor, newFolderPath )
                               && TypeScriptContext.DeleteFolder( monitor, tempFolderPath );
                    }

                    static bool AddImportAndConclude( IActivityMonitor monitor,
                                                        NormalizedPath filePath,
                                                        bool success,
                                                        ref string app,
                                                        string importStatement )
                    {
                        app = app.ReplaceLineEndings();
                        var lines = app.Split( Environment.NewLine ).ToList();
                        success &= AddImportStatement( monitor, lines, importStatement );
                        if( !success )
                        {
                            monitor.CloseGroup( "Failed to transform file. Leaving it as-is." );
                        }
                        else
                        {
                            monitor.CloseGroup( "File has been transformed." );
                            File.WriteAllLines( filePath, lines );
                        }
                        return success;

                        static bool AddImportStatement( IActivityMonitor monitor, List<string> lines, string importStatement )
                        {
                            int idx = lines.FindLastIndex( l => l.StartsWith( "import " ) );
                            if( idx < 0 )
                            {
                                monitor.Warn( "Unable to find an 'import ...' line." );
                                return false;
                            }
                            else
                            {
                                lines[idx] = lines[idx] + Environment.NewLine + importStatement;
                            }
                            return true;
                        }
                    }
                }
            }
        }

        bool ITSCodeGenerator.OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

        bool ITSCodeGenerator.OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute': no need (single shot), we keep the interpreted regex.

        void OnBeforeIntegration( object? sender, TypeScriptIntegrationContext.BeforeEventArgs e )
        {
            // Adds @angular/platform-browser-dynamic as dev dependency if not already here (jest requirement).
            e.AddOrUpdateTargetProjectDependency( "@angular/platform-browser-dynamic",
                                                  new SVersionBound( SVersion.Create( 20, 2, 2 ), SVersionLock.LockMajor, PackageQuality.Stable ),
                                                  DependencyKind.Dependency );

            // Adds the jest-preset-angular "15.0.0" if not alreay here.
            e.AddOrUpdateTargetProjectDependency( "jest-preset-angular",
                                                  new SVersionBound( SVersion.Create( 15, 0, 0 ), SVersionLock.LockMajor, PackageQuality.Stable ),
                                                  DependencyKind.DevDependency );
            e.JestSetup = new AngularJestSetupHandler( e.IntegrationContext );
        }

        void OnAfterIntegration( object? sender, TypeScriptIntegrationContext.AfterEventArgs e )
        {
            // Awful implementations.
            // Waiting for transformers.
            TransformAngularJson( e.Monitor, e.TargetProjectPath.AppendPart( "angular.json" ) );

            static void TransformAngularJson( IActivityMonitor monitor, NormalizedPath angularJsonPath )
            {
                var text = File.ReadAllText( angularJsonPath );
                var m = Regex.Match( text, """
                            "assets"\s*:\s*\[\s*{
                            """ );
                if( m.Success )
                {
                    if( !text.AsSpan( m.Index + m.Length ).Contains( "ck-gen/ts-assets", StringComparison.Ordinal ) )
                    {
                        var start = text.Substring( 0, m.Index + m.Length - 1 );
                        start += """{ "glob": "**/*", "input": "ck-gen/ts-assets" }, """;
                        start += text.Substring( m.Index + m.Length - 1 );
                        File.WriteAllText( angularJsonPath, start );
                        monitor.Info( "Added ck-gen/ts-assets to angular.json assets." );
                    }
                }
            }
        }

#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'
    }
}


