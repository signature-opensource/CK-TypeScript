using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using CSemVer;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CK.TS.Angular.Engine
{
    public partial class AngularCodeGeneratorImpl
    {
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
                        DeleteFolder( monitor, newFolderPath, recursive: true );
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
                                                  [NotNullWhen(true)]out PackageJsonFile? newPackageJson )
                {
                    savedLatestDependencies = ImmutableArray<PackageDependency>.Empty;
                    newPackageJson = PackageJsonFile.ReadFile( monitor,
                                                               newFolderPath.AppendPart( "package.json" ),
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
                    if( !newPackageJson.Dependencies.AddOrUpdate( monitor, targetPackageJson.Dependencies.Values, false ) )
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
                            if( !DeleteFolder( monitor, conflictFolderPath, recursive: true ) )
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
                            monitor.Error( $"While moving '{currentTarget.Substring( targetProjectPathLength )}' to '{_conflictFolderName}' folder.", ex );
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
                        if( !targetPackageJson.Dependencies.AddOrUpdate( monitor, savedLatestDependencies, false ) )
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
                    DeleteFolder( monitor, tempFolderPath + "/.yarn", recursive: true );
                    monitor.Info( "Removing the now empty folder." );
                    return DeleteFolder( monitor, newFolderPath )
                           && DeleteFolder( monitor, tempFolderPath );
                }

                static bool DeleteFolder( IActivityMonitor monitor, NormalizedPath existingFolderPath, bool recursive = false )
                {
                    int retryCount = 0;
                    retry:
                    try
                    {
                        if( Directory.Exists( existingFolderPath ) )
                        {
                            Directory.Delete( existingFolderPath, recursive );
                        }

                        return true;
                    }
                    catch( Exception ex )
                    {
                        if( retryCount++ > 5 )
                        {
                            monitor.Error( $"Unable to delete folder '{existingFolderPath}'.", ex );
                            return false;
                        }
                        if( retryCount == 1 ) monitor.Warn( $"Deleting folder '{existingFolderPath}' failed. Retrying up tp 5 times." );
                        Thread.Sleep( retryCount * 50 );
                        goto retry;
                    }
                }
            }
        }

        void OnAfterIntegration( object? sender, TypeScriptIntegrationContext.AfterEventArgs e )
        {
            TransformAppComponent( e.Monitor, e.SrcFolderPath.Combine( "app/app.component.ts" ) );
            TransformAppConfig( e.Monitor, e.SrcFolderPath.Combine( "app/app.config.ts" ) );

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
                        app = app.ReplaceLineEndings();
                        var lines = app.Split( Environment.NewLine ).ToList();
                        success = AddImportStatement( monitor, lines );
                        if( !success )
                        {
                            monitor.CloseGroup( "Failed to transform file. Leaving it as-is." );
                        }
                        else
                        {
                            monitor.CloseGroup( "File has been transformed." );
                            File.WriteAllLines( appFilePath, lines );
                        }
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
                        app = app.ReplaceLineEndings();
                        var lines = app.Split( Environment.NewLine ).ToList();
                        success = AddImportStatement( monitor, lines );
                        if( !success )
                        {
                            monitor.CloseGroup( "Failed to transform file. Leaving it as-is." );
                        }
                        else
                        {
                            monitor.CloseGroup( "File has been transformed." );
                            File.WriteAllLines( configFilePath, lines );
                        }
                    }
                }
            }

            static bool AddImportStatement( IActivityMonitor monitor, List<string> lines )
            {
                int idx = lines.FindLastIndex( l => l.StartsWith( "import " ) );
                if( idx < 0 )
                {
                    monitor.Warn( "Unable to find an 'import ...' line." );
                    return false;
                }
                else
                {
                    lines[idx] = lines[idx] + Environment.NewLine + "import { CKGenAppModule } from '@local/ck-gen';";
                }
                return true;
            }
        }
    }
}
