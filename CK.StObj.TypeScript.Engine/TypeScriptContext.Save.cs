using CK.Core;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System;
using System.Linq;
using CK.TypeScript.CodeGen;
using System.Collections.Immutable;
using Microsoft.Extensions.Hosting;
using CSemVer;
using System.Formats.Asn1;
using System.Text;

namespace CK.Setup
{
    public sealed partial class TypeScriptContext
    {
        internal bool Save( IActivityMonitor monitor )
        {
            bool success = true;
            using( monitor.OpenInfo( $"Saving generated TypeScript for:{Environment.NewLine}{BinPathConfiguration.ToXml()}" ) )
            {
                var ckGenFolder = BinPathConfiguration.TargetProjectPath.AppendPart( "ck-gen" );
                var targetCKGenFolder = BinPathConfiguration.TargetCKGenPath;

                var saver = BinPathConfiguration.CKGenBuildMode
                            ? new BuildModeSaver( Root, targetCKGenFolder )
                            : new TypeScriptFileSaveStrategy( Root, targetCKGenFolder );
                // We want a root barrel for the generated module.
                Root.Root.EnsureBarrel();
                int? savedCount = Root.Save( monitor, saver );
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
                        // typescript by installing the BinPathConfiguration.AutomaticTypeScriptVersion typescript package.
                        var targetProjectPath = BinPathConfiguration.TargetProjectPath;
                        var projectJsonPath = targetProjectPath.AppendPart( "package.json" );
                        // The targetPackageJson is null if it cannot be read and empty for an unexisting one.
                        // We ignore the version bound check for the targetPackageJson because we update
                        // only the PeerDependencies of the /ck-gen: these versions MUST be synchronized.
                        var targetPackageJson = PackageJsonFile.ReadFile( monitor, projectJsonPath, ignoreVersionsBound: true );
                        if( targetPackageJson == null )
                        {
                            monitor.Error( $"The target Package.json cannot be read. This needs to be manually fixed." );
                            return false;
                        }
                        // Even if we don't know yet whether Yarn is installed, we lookup the Yarn typescript sdk version.
                        // Before compiling the ck-gen folder, we must ensure that the Yarn typescript sdk is installed: if it's not, package resolution fails miserably.
                        // We read the typeScriptSdkVersion here.
                        // We also return whether the target project has TypeScript installed: if yes, there's no need to "yarn add" it to
                        // the ck-gen project, "yarn install" is enough.
                        PackageDependency typeScriptDep = FindBestTypeScriptVersion( monitor, targetProjectPath, targetPackageJson,
                                                                                     out SVersion? typeScriptSdkVersion,
                                                                                     out bool targetProjectHasTypeScript );
                        // The code MAY have declared an incompatible version...
                        if( !saver.GeneratedDependencies.AddOrUpdate( monitor, typeScriptDep, cloneAddedDependency: false ) )
                        {
                            return false;
                        }
                        // It is necessarily here.
                        var final = saver.GeneratedDependencies["typescript"];
                        if( final != typeScriptDep )
                        {
                            if( final.DependencyKind != DependencyKind.DevDependency )
                            {
                                monitor.Warn( $"Some package declared \"typescript\" as a '{final.DependencyKind}'. This has been corrected to a DevDependency." );
                                // Come on, code! typescript is a dev dependency.
                                final.UnconditionalSetDependencyKind( DependencyKind.DevDependency );
                            }
                            if( final.Version != typeScriptDep.Version )
                            {
                                monitor.Warn( $"Some package declared \"typescript\" in version '{final.Version.ToNpmString()}'. Using it." );
                                typeScriptDep = final;
                            }
                        }
                        if( typeScriptSdkVersion != null )
                        {
                            WarnDiffTypeScriptSdkVersion( monitor, typeScriptSdkVersion, typeScriptDep.Version.Base );
                        }
                        switch( BinPathConfiguration.IntegrationMode )
                        {
                            case CKGenIntegrationMode.None:
                                monitor.Info( "Skipping any TypeScript project setup since IntegrationMode is None." );
                                break;
                            case CKGenIntegrationMode.NpmPackage:
                                success = NpmPackageIntegrate( monitor, ckGenFolder, BinPathConfiguration, saver, targetPackageJson, typeScriptDep, typeScriptSdkVersion );
                                break;
                            case CKGenIntegrationMode.Inline:
                                success = TSPathInlineIntegrate( monitor, ckGenFolder, BinPathConfiguration, saver, targetPackageJson, typeScriptDep, typeScriptSdkVersion );
                                break;
                        }
                    }
                }
                else success = false;
            }
            return success;
        }

        static void WarnDiffTypeScriptSdkVersion( IActivityMonitor monitor, SVersion typeScriptSdkVersion, SVersion targetTypeScriptVersion )
        {
            if( typeScriptSdkVersion != targetTypeScriptVersion )
            {
                monitor.Warn( $"The TypeScript version used by the Yarn sdk '{typeScriptSdkVersion}' differs from the selected one '{targetTypeScriptVersion}'.{Environment.NewLine}" +
                              $"This can lead to annoying issues such as import resolution failures." );
            }
        }

        PackageDependency FindBestTypeScriptVersion( IActivityMonitor monitor,
                                                     NormalizedPath targetProjectPath,
                                                     PackageJsonFile? targetPackageJson,
                                                     out SVersion? typeScriptSdkVersion,
                                                     out bool targetProjectHasTypeScript )
        {
            // Should we use ONLY this one if it exists?
            // Currently the target project version leads and we emit warinings... Because the idea is
            // to avoid changing the target project package.json.
            typeScriptSdkVersion = YarnHelper.GetYarnSdkTypeScriptVersion( monitor, targetProjectPath );

            targetProjectHasTypeScript = true;
            var source = "target project";
            var targetTypeScriptVersion = targetPackageJson?.Dependencies.GetValueOrDefault("typescript")?.Version;
            if( targetTypeScriptVersion is null )
            {
                targetProjectHasTypeScript = false;
                if( typeScriptSdkVersion == null )
                {
                    source = "BinPathConfiguration.AutomaticTypeScriptVersion property";
                    var parseResult = SVersionBound.NpmTryParse( BinPathConfiguration.AutomaticTypeScriptVersion );
                    Throw.DebugAssert( "The code defined version is necessarily valid.", parseResult.IsValid );
                    targetTypeScriptVersion = parseResult.Result;
                }
                else
                {
                    source = "Yarn TypeScript sdk";
                    targetTypeScriptVersion = new SVersionBound( typeScriptSdkVersion, SVersionLock.Lock, PackageQuality.Stable );
                }
            }
            monitor.Info( $"Considering TypeScript version '{targetTypeScriptVersion}' from {source}." );
            return new PackageDependency( "typescript", targetTypeScriptVersion.Value, DependencyKind.DevDependency );
        }

        static bool NpmPackageIntegrate( IActivityMonitor monitor,
                                         NormalizedPath ckGenFolder,
                                         TypeScriptBinPathAspectConfiguration config,
                                         TypeScriptFileSaveStrategy saver,
                                         PackageJsonFile targetPackageJson,
                                         PackageDependency typeScriptDep,
                                         SVersion? typeScriptSdkVersion )
        {
            Throw.DebugAssert( config.UseSrcFolder );
            // Generates "/ck-gen": "package.json", "tsconfig.json" and potentially "tsconfig-cjs.json" and "tsconfig-es6.json" files.
            // This may fail if there's an error in the dependencies declared by the code generator (in LibraryImport).
            if( !YarnHelper.SaveCKGenBuildConfig( monitor,
                                                  ckGenFolder,
                                                  saver.GeneratedDependencies,
                                                  config.ModuleSystem,
                                                  config.EnableTSProjectReferences ) )
            {
                return false;
            }
            var yarnPath = YarnHelper.GetYarnInstallPath( monitor,
                                                            config.TargetProjectPath,
                                                            config.AutoInstallYarn );
            if( !yarnPath.HasValue ) return false;

            // The workspace dependency.
            PackageDependency ckGenDep = new PackageDependency( "@local/ck-gen", SVersionBound.None, DependencyKind.DevDependency );

            // Chicken & egg issue here:
            // "yarn sdks vscode" or "yarn sdks base" will not install typescript support unless "typescript" appears in the package.json
            // (and "yarn sdks typescript" is not supported).
            // So we must ensure that when starting from scratch, the target package.json has typescript installed.
            if( targetPackageJson.IsEmpty )
            {
                monitor.Info( $"Creating a minimal package.json with typescript development dependency '{typeScriptDep.Version.ToNpmString()}'." );
                targetPackageJson.Dependencies.AddOrUpdate( monitor, typeScriptDep, cloneAddedDependency: false );
                targetPackageJson.Name = targetPackageJson.SafeName;
                targetPackageJson.Private = true;
                targetPackageJson.Workspaces.Add( "ck-gen" );
                targetPackageJson.Dependencies.AddOrUpdate( monitor, ckGenDep, cloneAddedDependency: false );
                targetPackageJson.Save();
            }

            bool success = true;
            // We have a yarn, we can build "@local/ck-gen".
            // If the targetPackageJson is empty, a minimal one is created but if it is on error, we
            // don't replace it!
            using( monitor.OpenInfo( $"Building '@local/ck-gen' package..." ) )
            {
                // Ensuring that TypeScript is installed in /ck-gen.
                // Install the /ck-gen dependencies.
                success &= YarnHelper.DoRunYarn( monitor, ckGenFolder, "install", yarnPath.Value );
                // If the yarn type script sdk is not installed (target project level), we must install it before
                // trying to build the /ck-gen.
                if( typeScriptSdkVersion == null )
                {
                    using( monitor.OpenInfo( $"Yarn TypeScript sdk is not installed. Installing it with{(config.AutoInstallVSCodeSupport ? "" : "out")} VSCode support." ) )
                    {
                        success &= YarnHelper.InstallYarnSdkSupport( monitor,
                                                                     config.TargetProjectPath,
                                                                     config.AutoInstallVSCodeSupport,
                                                                     yarnPath.Value,
                                                                     ref typeScriptSdkVersion );
                        if( success )
                        {
                            Throw.DebugAssert( "The [NotNullWhen(true)] is ignored.", typeScriptSdkVersion != null );
                            WarnDiffTypeScriptSdkVersion( monitor, typeScriptSdkVersion, typeScriptDep.Version.Base );
                        }
                    }
                }
                monitor.CloseGroup( success ? "Success." : "Failed." );
            }

            // Even if the build will fail, if there is a targetPackageJson we configure its content.
            // We always ensure that the workspaces:["ck-gen"] and the "@local/ck-gen" dependency are here
            // and propagate the PeerDependencies from /ck-gen to the target project.
            bool shouldRunYarnInstall = false;
            int changeTracker = targetPackageJson.Dependencies.ChangeTracker;
            if( !targetPackageJson.Workspaces.Contains( "*" ) && targetPackageJson.Workspaces.Add( "ck-gen" ) )
            {
                shouldRunYarnInstall = true;
                monitor.Info( $"Added \"ck-gen\" workspace." );
            }
            if( !targetPackageJson.Dependencies.TryGetValue( "@local/ck-gen", out var ck )
                || !ck.IsWorkspaceDependency
                || ck.DependencyKind != DependencyKind.DevDependency )
            {
                shouldRunYarnInstall = true;
                if( ck == null )
                {
                    targetPackageJson.Dependencies.AddOrUpdate( monitor, ckGenDep, false );
                    monitor.Info( $"Added \"@local/ck-gen\" as a workspace development dependency." );
                }
                else
                {
                    ck.UnconditionalSetVersion( SVersionBound.None );
                    ck.UnconditionalSetDependencyKind( DependencyKind.DevDependency );
                    monitor.Info( $"Fixed \"@local/ck-gen\" as a workspace development dependency." );
                }
            }
            // Propagates the PeerDependencies from /ck-gen to the target project.
            var peerDependencies = saver.GeneratedDependencies.Values.Where( d => d.DependencyKind is DependencyKind.PeerDependency ).ToList();
            if( peerDependencies.Count > 0 )
            {
                using( monitor.OpenInfo( $"Propagating {peerDependencies.Count} peer dependencies from /ck-gen to target project." ) )
                {
                    // There is no reason for this to fail because we setup the targetPackageJson.Dependencies
                    // to ignore version bounds.
                    success &= targetPackageJson.Dependencies.UpdateDependencies( monitor, peerDependencies );
                    shouldRunYarnInstall = changeTracker != targetPackageJson.Dependencies.ChangeTracker;
                }
            }
            targetPackageJson.Save();
            // Only try a compilation of the /ck-gen if no error occurred so far.
            if( success )
            {
                success = YarnHelper.DoRunYarn( monitor, ckGenFolder, "run build", yarnPath.Value );
            }

            if( config.EnsureTestSupport )
            {
                // If we must ensure test support, we consider that as soon as a "test" script is available
                // we are done: the goal is to support "yarn test", Jest is our default test framework but is
                // not required.
                PackageDependency? actualTypeScriptDep;
                if( targetPackageJson.Dependencies.TryGetValue( "typescript", out actualTypeScriptDep )
                    && targetPackageJson.Scripts.TryGetValue( "test", out var testCommand )
                    && testCommand != "jest" )
                {
                    monitor.Warn( $"TypeScript test script command '{testCommand}' is not 'jest'. Skipping Jest tests setup." );
                }
                else
                {
                    using( monitor.OpenInfo( $"Ensuring TypeScript test with Jest." ) )
                    {
                        // Always setup jest even on error.
                        YarnHelper.EnsureSampleJestTestInSrcFolder( monitor, config.TargetProjectPath );
                        YarnHelper.SetupJestConfigFile( monitor, config.TargetProjectPath );
                        targetPackageJson.Scripts["test"] = "jest";
                        targetPackageJson.Save();

                        int dCount = targetPackageJson.Dependencies.Count;
                        IEnumerable<string> toInstall = new string[]
                        {
                                            "jest",
                                            "ts-jest",
                                            "@types/jest",
                                            "@types/node",
                                            // Because we use testEnvironment: 'jsdom' (this package is required from jest v29).
                                            "jest-environment-jsdom"
                        };
                        toInstall = toInstall.Where( p => !targetPackageJson.Dependencies.ContainsKey( p ) );
                        // Don't touch the exisiting typescript.
                        if( actualTypeScriptDep == null )
                        {
                            toInstall = toInstall.Append( $"typescript@{typeScriptDep.Version.ToNpmString()}" );
                        }
                        if( toInstall.Any() )
                        {
                            success &= YarnHelper.DoRunYarn( monitor, config.TargetProjectPath, $"add --prefer-dev {toInstall.Concatenate( " " )}", yarnPath.Value );
                            shouldRunYarnInstall = false;
                        }
                        else
                        {
                            monitor.Trace( "No missing package to install." );
                        }
                    }
                }

                if( shouldRunYarnInstall )
                {
                    success &= YarnHelper.DoRunYarn( monitor, config.TargetProjectPath, "install", yarnPath.Value );
                }
            }
            return success;
        }

        static bool TSPathInlineIntegrate( IActivityMonitor monitor,
                                           NormalizedPath ckGenFolder,
                                           TypeScriptBinPathAspectConfiguration config,
                                           TypeScriptFileSaveStrategy saver,
                                           PackageJsonFile targetPackageJson,
                                           PackageDependency typeScriptDep,
                                           SVersion? typeScriptSdkVersion )
        {
            if( config.UseSrcFolder )
            {
                // If this fails, we don't care: this is purely informational.
                YarnHelper.SaveCKGenBuildConfig( monitor,
                                                 ckGenFolder,
                                                 saver.GeneratedDependencies,
                                                 config.ModuleSystem,
                                                 config.EnableTSProjectReferences,
                                                 filePrefix: "CouldBe." );
            }
            var yarnPath = YarnHelper.GetYarnInstallPath( monitor,
                                                            config.TargetProjectPath,
                                                            config.AutoInstallYarn );
            if( !yarnPath.HasValue ) return false;

            // Chicken & egg issue here:
            // "yarn sdks vscode" or "yarn sdks base" will not install typescript support unless "typescript" appears in the package.json
            // (and "yarn sdks typescript" is not supported).
            // So we must ensure that when starting from scratch, the target package.json has typescript installed.
            bool success = true;
            if( targetPackageJson.IsEmpty )
            {
                monitor.Info( $"Creating a minimal package.json with typescript development dependency '{typeScriptDep.Version.ToNpmString()}'." );
                targetPackageJson.Dependencies.AddOrUpdate( monitor, typeScriptDep, cloneAddedDependency: false );
                targetPackageJson.Name = targetPackageJson.SafeName;
                targetPackageJson.Private = true;
                targetPackageJson.Save();
                // Ensuring that TypeScript is installed.
                success &= YarnHelper.DoRunYarn( monitor, config.TargetProjectPath, "install", yarnPath.Value );
            }
            // If the yarn type script sdk is not installed (target project level), we must install it before
            // trying to build anything.
            if( typeScriptSdkVersion == null )
            {
                using( monitor.OpenInfo( $"Yarn TypeScript sdk is not installed. Installing it with{(config.AutoInstallVSCodeSupport ? "" : "out")} VSCode support." ) )
                {
                    success &= YarnHelper.InstallYarnSdkSupport( monitor,
                                                                 config.TargetProjectPath,
                                                                 config.AutoInstallVSCodeSupport,
                                                                 yarnPath.Value,
                                                                 ref typeScriptSdkVersion );
                    if( success )
                    {
                        Throw.DebugAssert( "The [NotNullWhen(true)] is ignored.", typeScriptSdkVersion != null );
                        WarnDiffTypeScriptSdkVersion( monitor, typeScriptSdkVersion, typeScriptDep.Version.Base );
                    }
                }
            }
            // Remove NpmPackage mode.
            bool shouldRunYarnInstall = false;
            if( targetPackageJson.Dependencies.Remove( "@local/ck-gen" ) )
            {
                monitor.Info( "Removed '@local/ck-gen' package dependency (NpmPackage integration mode)." );
                shouldRunYarnInstall = true;
            }
            if( targetPackageJson.Workspaces.Remove( "ck-gen" ) )
            {
                monitor.Info( $"Removed 'ck-gen' Yarn workspace (NpmPackage integration mode)." );
                shouldRunYarnInstall = true;
            }
            // Propagates the dependencies required by generated code to the target project.
            int changeTracker = targetPackageJson.Dependencies.ChangeTracker;
            using( monitor.OpenInfo( $"Updating {saver.GeneratedDependencies.Count} dependencies." ) )
            {
                // There is no reason for this to fail because we setup the targetPackageJson.Dependencies
                // to ignore version bounds.
                success &= targetPackageJson.Dependencies.UpdateDependencies( monitor, saver.GeneratedDependencies.Values );
                shouldRunYarnInstall = changeTracker != targetPackageJson.Dependencies.ChangeTracker;
            }
            targetPackageJson.Save();
            if( shouldRunYarnInstall )
            {
                success &= YarnHelper.DoRunYarn( monitor, config.TargetProjectPath, "install", yarnPath.Value );
            }

            // Handle tsConfig.json. If we cannot read it, give up.
            var tsConfig = TSConfigJsonFile.ReadFile( monitor, config.TargetProjectPath.AppendPart( "tsConfig.json" ) );
            if( tsConfig == null ) return false;

            // If the tsConfig is empty (it doesn't exist), let's create a default one.
            // If the tsc --init fails, ignores and continue (there sould be no reason for this to fail as we checked
            // that no tsConfig.json already exists).
            if( tsConfig.IsEmpty )
            {
                // No need to log: The "tsc --init" will appear in the logs. 
                if( YarnHelper.DoRunYarn( monitor, config.TargetProjectPath, "tsc --init", yarnPath.Value ) )
                {
                    // Read it back.
                    tsConfig = TSConfigJsonFile.ReadFile( monitor, config.TargetProjectPath.AppendPart( "tsConfig.json" ) );
                    if( tsConfig == null ) return false;
                }
            }

            // Ensure that the compilerOptions:paths has the "@local/ck-gen/*": ["./ck-gen/src/*"] entry (TSPathInline).
            if( !tsConfig.ResolvedBaseUrl.TryGetRelativePathTo( config.TargetCKGenPath, out NormalizedPath mapping ) )
            {
                monitor.Error( $"Unable to compute relative path from tsConfig.json baseUrl '{tsConfig.ResolvedBaseUrl}' to {config.TargetCKGenPath}." );
                return false;
            }
            bool shouldSaveTSConfig = false;
            mapping = mapping + "/*";
            if( !tsConfig.CompilerOptionsPaths.TryGetValue( "@local/ck-gen/*", out var mappings )
                || !mappings.Contains( mapping ) )
            {
                tsConfig.CompilerOptionsPaths["@local/ck-gen/*"] = new HashSet<string> { mapping };
                monitor.Info( $"CompilerOption Paths mapped \"@local/ck-gen/*\" to \"{mapping}\"." );
                shouldSaveTSConfig = true;
            }
            // Remove compilerOptions:paths "@local/ck-gen" entry (TSPath integration mode).
            if( tsConfig.CompilerOptionsPaths.Remove( "@local/ck-gen" ) )
            {
                monitor.Info( $$"""Removed "compilerOptions": { "paths": { "@local/ck-gen": ...} } mapping (TSPath integration mode).""" );
                shouldSaveTSConfig = true;
            }
            if( shouldSaveTSConfig )
            {
                tsConfig.Save();
            }

            if( config.EnsureTestSupport )
            {
                // If we must ensure test support, we consider that as soon as a "test" script is available
                // we are done: the goal is to support "yarn test", Jest is our default test framework but is
                // not required.
                if( targetPackageJson.Dependencies.TryGetValue( "typescript", out PackageDependency? actualTypeScriptDep )
                    && targetPackageJson.Scripts.TryGetValue( "test", out var testCommand )
                    && testCommand != "jest" )
                {
                    monitor.Warn( $"TypeScript test script command '{testCommand}' is not 'jest'. Skipping Jest tests setup." );
                }
                else
                {
                    using( monitor.OpenInfo( $"Ensuring TypeScript test with Jest." ) )
                    {
                        // Always setup jest even on error.
                        YarnHelper.EnsureSampleJestTestInSrcFolder( monitor, config.TargetProjectPath );
                        YarnHelper.SetupJestConfigFile( monitor, config.TargetProjectPath );
                        targetPackageJson.Scripts["test"] = "jest";
                        targetPackageJson.Save();

                        int dCount = targetPackageJson.Dependencies.Count;
                        IEnumerable<string> toInstall =
                        [
                                            "jest",
                                            "ts-jest",
                                            "@types/jest",
                                            "@types/node",
                                            // Because we use testEnvironment: 'jsdom' (this package is required by jest v29+).
                                            "jest-environment-jsdom"
                        ];
                        toInstall = toInstall.Where( p => !targetPackageJson.Dependencies.ContainsKey( p ) );
                        // Don't touch the exisitng typescript.
                        if( actualTypeScriptDep == null )
                        {
                            toInstall = toInstall.Append( $"typescript@{typeScriptDep.Version.ToNpmString()}" );
                        }
                        if( toInstall.Any() )
                        {
                            success &= YarnHelper.DoRunYarn( monitor, config.TargetProjectPath, $"add --prefer-dev {toInstall.Concatenate( " " )}", yarnPath.Value );
                            shouldRunYarnInstall = false;
                        }
                        else
                        {
                            monitor.Trace( "No missing package to install." );
                        }
                    }
                }

                if( shouldRunYarnInstall )
                {
                    success &= YarnHelper.DoRunYarn( monitor, config.TargetProjectPath, "install", yarnPath.Value );
                }
            }
            return success;
        }
    }
}
