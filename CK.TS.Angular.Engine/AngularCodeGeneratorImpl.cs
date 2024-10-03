using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using CSemVer;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System;
using System.Linq;

namespace CK.TS.Angular.Engine;

public partial class AngularCodeGeneratorImpl : ITSCodeGeneratorFactory
{
    const string _defaultAngularCliVersion = "^18.2.0";
    const string _conflictFolderName = "_ckConflict_";

    ITSCodeGenerator? ITSCodeGeneratorFactory.CreateTypeScriptGenerator( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
    {
        if( initializer.BinPathConfiguration.IntegrationMode != CKGenIntegrationMode.Inline )
        {
            monitor.Warn( $"Angular application requires Inline IntegrationMode. '{initializer.BinPathConfiguration}' mode is not supported, skipping Angular support." );
            return ITSCodeGenerator.Empty;
        }
        return new CodeGen();
    }

    sealed class CodeGen : ITSCodeGenerator
    {
        [AllowNull] LibraryImport _angularCore;
        [AllowNull] ITSFileType _ckGenAppModule;
        [AllowNull] ITSCodePart _importModulePart;
        [AllowNull] ITSCodePart _exportModulePart;
        [AllowNull] ITSCodePart _providerPart;
        [AllowNull] ITSCodePart _routesPart;

        public bool StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
        {
            _angularCore = context.Root.LibraryManager.RegisterLibrary( monitor, "@angular/core", DependencyKind.Dependency );

            var f = context.Root.Root.FindOrCreateTypeScriptFile( "CK/Angular/CKGenAppModule.ts" );
            f.Imports.EnsureImportFromLibrary( _angularCore, "NgModule", "Provider" );
            _ckGenAppModule = f.CreateType( "CKGenAppModule", null, null );
            _ckGenAppModule.TypePart.Append( """
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
                    static Providers : Provider[] = [

                """ )
                .InsertPart( out _providerPart )
                .Append( """

                    ];
                """ );
            
            var r = context.Root.Root.FindOrCreateTypeScriptFile( "CK/Angular/routes.ts" );
            r.Body.Append( """
                export default [

                """ )
                .InsertPart( out _routesPart )
                .Append( """

                ];
                """ );

            Throw.DebugAssert( "Inline mode => IntegrationContext.", context.IntegrationContext != null );
            context.IntegrationContext.OnBeforeIntegration += OnBeforeIntegration;
            context.IntegrationContext.OnAfterIntegration += OnAfterIntegration;

            return true;
        }
        bool ITSCodeGenerator.OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

        bool ITSCodeGenerator.OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

        void OnBeforeIntegration( object? sender, TypeScriptIntegrationContext.BeforeEventArgs e )
        {
            if( ValidateConfiguration( e.Monitor, e.IntegrationContext.Configuration ) )
            {
                var angularJsonPath = e.TargetProjectPath.AppendPart( "angular.json" );
                if( File.Exists( angularJsonPath ) )
                {
                    e.Monitor.Info( "Found existing 'angular.json' file." );
                }
                else
                {
                    CreateAngularApp( e.Monitor, e );
                }
                // Adds the jest-preset-angular if not alreay here.
                e.AddOrUpdateTargetProjectDependency( "jest-preset-angular", SVersionBound.All, DependencyKind.DevDependency );
                e.JestSetup = new AngularJestSetupHandler( e.IntegrationContext );
            }

            static bool ValidateConfiguration( IActivityMonitor monitor, TypeScriptBinPathAspectConfiguration configuration )
            {
                if( !configuration.AutoInstallJest )
                {
                    monitor.Warn( $"Setting AutoInstallJest to true (Angular application must always have test support)." );
                    configuration.AutoInstallJest = true;
                }
                return true;
            }

            static bool CreateAngularApp( IActivityMonitor monitor, TypeScriptIntegrationContext.BeforeEventArgs e )
            {
                using( monitor.OpenInfo( "No 'angular.json' found, creating a default Angular application." ) )
                {
                    // This doesn't work: the npx.cmd looks for a npx-cli.js in the nodemodules/ of the workingDirectory instead
                    // of the nodemodules/ of the nodes/ or npm/ folder (the %~dp0 variable).
                    //
                    // e.RunProcess( "npx.cmd",
                    //               $"--package @angular/cli@18 ng new {e.TargetProjectPath.LastPart} --style=less --package-manager=yarn --interactive=false --standalone=true --minimal=true --skip-install=true --skip-git=true",
                    //               workingDirectory: e.TargetProjectPath.RemoveLastPart() );
                    //

                    NormalizedPath targetProjectPath = e.TargetProjectPath;
                    NormalizedPath tempFolderPath = targetProjectPath.AppendPart( "_ckNew_" );
                    NormalizedPath newFolderPath = tempFolderPath.AppendPart( targetProjectPath.LastPart.Replace( ' ', '_' ) );
                    NormalizedPath tempPackageJsonPath = tempFolderPath.AppendPart( "package.json" );
                    PackageJsonFile targetPackageJson = e.IntegrationContext.TargetPackageJson;

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

                    return CreateNewAngular( monitor, e, tempFolderPath, newFolderPath, tempPackageJsonPath )
                           && UpdateNewPackageJson( monitor, newFolderPath, targetPackageJson, out IList<PackageDependency> savedLatestDependencies, out var newPackageJson )
                           && RemoveUselessAngularJsonTestSection( monitor, newFolderPath, newPackageJson )
                           && LiftContent( monitor, targetProjectPath, newFolderPath )
                           && ReloadTargetTSConfigAndPackageJson( monitor, e.IntegrationContext.TSConfigJson, targetPackageJson, savedLatestDependencies )
                           && DeleteNewFolder( monitor, tempFolderPath, newFolderPath, tempPackageJsonPath );
                }

                static bool CreateNewAngular( IActivityMonitor monitor,
                                              TypeScriptIntegrationContext.BeforeEventArgs e,
                                              NormalizedPath tempFolderPath,
                                              NormalizedPath newFolderPath,
                                              NormalizedPath tempPackageJsonPath )
                {
                    using( monitor.OpenInfo( $"Initialize a new Angular '{newFolderPath.LastPart}' project in the folder '{newFolderPath}'." ) )
                    {
                        Directory.CreateDirectory( tempFolderPath );
                        if( !e.ConfiguredLibraries.TryGetValue( "@angular/cli", out var angularCliVersion ) )
                        {
                            var parseResult = SVersionBound.NpmTryParse( _defaultAngularCliVersion );
                            Throw.DebugAssert( "The version defined in code is necessarily valid.", parseResult.IsValid );
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
                        if( !e.RunYarn( $"add @angular/cli@{angularCliVersion.ToNpmString()}",
                                        workingDirectory: tempFolderPath ) )
                        {
                            return false;
                        }
                        if( !e.RunYarn( $"ng new {newFolderPath.LastPart} --style=less --package-manager=yarn --interactive=false --standalone=true --skip-install=true --skip-git=true",
                                        workingDirectory: tempFolderPath ) )
                        {
                            return false;
                        }
                        return true;
                    }
                }

                static bool RemoveUselessAngularJsonTestSection( IActivityMonitor monitor, NormalizedPath newFolderPath, PackageJsonFile newPackageJson )
                {
                    // LOL! This is absolutely insane.
                    // (But STJ is really bad at this. I don't want a dependency an have better thing to do right now.)
                    NormalizedPath filePath = newFolderPath.AppendPart( "angular.json" );
                    var text = File.ReadAllText( filePath ).ReplaceLineEndings();
                    var start = """
                    ,
                            "test": {
                    """.ReplaceLineEndings();
                    int idxStart = text.IndexOf( start );
                    if( idxStart > 0 )
                    {
                        var idxEnd = text.IndexOf( Environment.NewLine + "        }", idxStart );
                        if( idxEnd > 0 )
                        {
                            text = text.Remove( idxStart, idxEnd - idxStart + 9 + Environment.NewLine.Length );
                            File.WriteAllText( filePath, text );
                            monitor.Info( "Removed useless \"test\" section from 'angular.json' file." );
                            return true;
                        }
                    }
                    monitor.Warn( "Unable to locate the \"test\" section in 'angular.json' file." );
                    return true;
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
                            // Consider the root _new_/XXX/ level by using Directory.Move that handles
                            // folders and files.
                            // On conflict, the /_ckConflict_ will contain the whole sub folder ("/src" for example).
                            foreach( var entry in Directory.EnumerateFileSystemEntries( newFolderPath ) )
                            {
                                currentTarget = entry.Remove( idxRemove, lenRemove );
                                if( Path.Exists( currentTarget ) )
                                {
                                    MoveToConflicts( monitor, ref hasConflict, targetProjectPath.Path.Length, currentTarget, conflictFolderPath );
                                }
                                Directory.Move( entry, currentTarget );
                            }
                        }
                        catch( Exception ex )
                        {
                            monitor.Error( $"While updating '{currentTarget}'.", ex );
                            return false;
                        }
                        return true;
                    }

                    static bool MoveToConflicts( IActivityMonitor monitor, ref bool hasConflict, int targetProjectPathLength, string currentTarget, NormalizedPath conflictFolderPath )
                    {
                        if( !hasConflict )
                        {
                            hasConflict = true;
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
            }
        }

        void OnAfterIntegration( object? sender, TypeScriptIntegrationContext.AfterEventArgs e )
        {
            // Awful implementation.
            // Waiting for transformers.
            TransformAppComponent( e.Monitor, e.SrcFolderPath.Combine( "app/app.component.ts" ) );
            TransformAppConfig( e.Monitor, e.SrcFolderPath.Combine( "app/app.config.ts" ) );
            TransformAppRoutes( e.Monitor, e.SrcFolderPath.Combine( "app/app.routes.ts" ) );

            static void TransformAppComponent( IActivityMonitor monitor, NormalizedPath appFilePath )
            {
                var app = File.ReadAllText( appFilePath );
                if( app.Contains( "import { CKGenAppModule } from '@local/ck-gen';" ) )
                {
                    monitor.Trace( "File 'src/app/component.ts' imports the CKGenAppModule. Skipping transformation." );
                }
                else
                {
                    using( monitor.OpenInfo( "Transforming file 'src/app/component.ts'." ) )
                    {
                        bool success = AddInImports( monitor, ref app );
                        AddImportAndConclude( monitor, appFilePath, success, ref app, "import { CKGenAppModule } from '@local/ck-gen';" );
                    }
                }

                static bool AddInImports( IActivityMonitor monitor, ref string app )
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

            static void TransformAppConfig( IActivityMonitor monitor, NormalizedPath configFilePath )
            {
                var app = File.ReadAllText( configFilePath );
                if( app.Contains( "import { CKGenAppModule } from '@local/ck-gen';" ) )
                {
                    monitor.Trace( "File 'src/app/app.config.ts' imports the CKGenAppModule. Skipping transformation." );
                }
                else
                {
                    using( monitor.OpenInfo( "Transforming file 'src/app/app.config.ts'." ) )
                    {
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
                        AddImportAndConclude( monitor, configFilePath, success, ref app, "import { CKGenAppModule } from '@local/ck-gen';" );
                    }
                }
            }

            static void TransformAppRoutes( IActivityMonitor monitor, NormalizedPath routesFilePath )
            {
                var app = File.ReadAllText( routesFilePath );
                if( app.Contains( "import CKGenRoutes from '@local/ck-gen/CK/Angular/routes';" ) )
                {
                    monitor.Trace( "File 'src/app/app.routes.ts' imports the CKGenRoutes. Skipping transformation." );
                }
                else
                {
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
                            if( !NeedComma( app, idx, out var needComma) )
                            {
                                monitor.Warn( "Unable to find te start of the array." );
                                success = false;
                            }
                            else
                            {
                                app = app.Insert( idx, $"{Environment.NewLine}{(needComma?',':' ')}...CKGenRoutes{Environment.NewLine}" );
                                monitor.Info( "Added '...CKGenRoutes' in routes." );
                            }
                        }
                        AddImportAndConclude( monitor, routesFilePath, success, ref app, "import CKGenRoutes from '@local/ck-gen/CK/Angular/routes';" );
                    }
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

            static void AddImportAndConclude( IActivityMonitor monitor, NormalizedPath filePath, bool success, ref string app, string importStatement )
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
