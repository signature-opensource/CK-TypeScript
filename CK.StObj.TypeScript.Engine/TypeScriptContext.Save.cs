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

        sealed class BuildModeSaver : TypeScriptFileSaveStrategy
        {
            List<(NormalizedPath Gen, OriginResource? Origin)>? _clashes;

            public BuildModeSaver( TypeScriptRoot root, NormalizedPath targetPath )
                : base( root, targetPath, withCleanupFiles: true )
            {
            }

            public override void SaveFile( IActivityMonitor monitor, TypeScriptFile file, NormalizedPath filePath )
            {
                var fInfo = new FileInfo( filePath );
                if( fInfo.Exists )
                {
                    using var fTxt = fInfo.OpenText();
                    var existing = fTxt.ReadToEnd();
                    var newOne = file.GetCurrentText();
                    if( existing != newOne )
                    {
                        _clashes ??= new List<(NormalizedPath,OriginResource?)>();
                        _clashes.Add( (file.Folder.Path.AppendPart( file.Name ), file.Origin) );
                        var filePathGen = filePath.Path + ".G.ts";
                        monitor.Trace( $"Saving '{file.Name}.G.ts'." );
                        File.WriteAllText( filePathGen, file.GetCurrentText() );
                        CleanupFiles?.Remove( filePath );
                        // Avoid deleting the generated file if it already exists.
                        CleanupFiles?.Remove( filePathGen );
                        return;
                    }
                }
                base.SaveFile( monitor, file, filePath );
            }

            public override int? Finalize( IActivityMonitor monitor, int? savedCount )
            {
                if( _clashes != null )
                {
                    using( monitor.OpenError( $"BuildMode: {_clashes.Count} files have been generated differently than the existing one:" ) )
                    {
                        var b = new StringBuilder();
                        foreach( var clash in _clashes.GroupBy( c => c.Origin?.Assembly ) )
                        {
                            if( clash.Key == null )
                            {
                                b.AppendLine( "> (unknwon assembly):" );
                                foreach( var c in clash )
                                {
                                    b.Append( "   " ).AppendLine( c.Gen );
                                }
                            }
                            else
                            {
                                b.Append( "> Assembly: " ).Append( clash.Key.GetName().Name ).Append(':').AppendLine();
                                foreach( var c in clash )
                                {
                                    b.Append( "   " ).Append( c.Gen ).Append( " <= " ).AppendLine( c.Origin!.ResourceName );
                                }
                            }
                        }
                        monitor.Trace( b.ToString() );
                    }
                    base.Finalize( monitor, savedCount );
                    return null;
                }
                return base.Finalize( monitor, savedCount );
            }
        }

        internal bool Save( IActivityMonitor monitor )
        {
            bool success = true;
            using( monitor.OpenInfo( $"Saving generated TypeScript for:{Environment.NewLine}{BinPathConfiguration.ToXml()}" ) )
            {
                var ckGenFolder = BinPathConfiguration.TargetProjectPath.AppendPart( "ck-gen" );
                var ckGenFolderSrc = ckGenFolder.AppendPart( "src" );

                var saver = BinPathConfiguration.CKGenBuildMode
                            ? new BuildModeSaver( Root, ckGenFolderSrc )
                            : new TypeScriptFileSaveStrategy( Root, ckGenFolderSrc );
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
                            CheckTypeScriptSdkVersion( monitor, typeScriptSdkVersion, typeScriptDep.Version.Base );
                        }

                        // Generates "/ck-gen": "package.json", "tsconfig.json" and potentially "tsconfig-cjs.json" and "tsconfig-es6.json" files.
                        // This may fail if there's an error in the dependencies declared by the code generator (in LibraryImport).
                        if( YarnHelper.SaveCKGenBuildConfig( monitor,
                                                             ckGenFolder,
                                                             saver.GeneratedDependencies,
                                                             BinPathConfiguration.ModuleSystem,
                                                             BinPathConfiguration.EnableTSProjectReferences ) )
                        {
                            if( BinPathConfiguration.SkipTypeScriptTooling )
                            {
                                monitor.Info( "Skipping any TypeScript project setup since SkipTypeScriptTooling is true." );
                            }
                            else
                            {
                                success = InstallTypeScriptTooling( monitor,
                                                                    ckGenFolder,
                                                                    targetProjectPath,
                                                                    targetPackageJson,
                                                                    typeScriptSdkVersion,
                                                                    typeScriptDep,
                                                                    saver.GeneratedDependencies );
                            }
                        }
                        else success = false;
                    }
                }
                else success = false;
            }
            return success;
        }

        bool InstallTypeScriptTooling( IActivityMonitor monitor,
                                       NormalizedPath ckGenFolder,
                                       NormalizedPath targetProjectPath,
                                       PackageJsonFile? targetPackageJson,
                                       SVersion? typeScriptSdkVersion,
                                       PackageDependency typeScriptDep,
                                       DependencyCollection generatedDependencies )
        {
            var yarnPath = YarnHelper.GetYarnInstallPath( monitor,
                                                          targetProjectPath,
                                                          BinPathConfiguration.AutoInstallYarn );
            if( !yarnPath.HasValue ) return false;

            // The workspace dependency.
            PackageDependency ckGenDep = new PackageDependency( "@local/ck-gen", SVersionBound.None, DependencyKind.DevDependency );

            // If the targetPackageJson is null (reading error), we nevertheless try to build the ck-gen.
            // We do this to be able to work in the target project if possible.
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
                    // Chicken & egg issue here:
                    // "yarn sdks vscode" or "yarn sdks base" will not install typescript support unless "typescript" appears in the package.json
                    // (and "yarn sdks typescript" is not supported).
                    // So we must ensure that when starting from scratch, the target package.json has typescript installed.
                    // (When starting from scratch: no error but no package.json in target project folder.)
                    // If we failed to read it, we try...
                    if( targetPackageJson != null && targetPackageJson.IsEmpty )
                    {
                        monitor.Info( $"Creating a minimal package.json with typescript development dependency '{typeScriptDep.Version.ToNpmString()}'." );
                        targetPackageJson.Dependencies.AddOrUpdate( monitor, typeScriptDep, cloneAddedDependency: false );
                        targetPackageJson.Name = targetPackageJson.SafeName;
                        targetPackageJson.Private = true;
                        targetPackageJson.Workspaces.Add( "ck-gen" );
                        targetPackageJson.Dependencies.AddOrUpdate( monitor, ckGenDep, cloneAddedDependency: false );
                        targetPackageJson.Save();
                    }
                    using( monitor.OpenInfo( $"Yarn TypeScript sdk is not installed. Installing it with{(BinPathConfiguration.AutoInstallVSCodeSupport ? "" : "out")} VSCode support." ) )
                    {
                        success &= YarnHelper.InstallYarnSdkSupport( monitor,
                                                                     targetProjectPath,
                                                                     BinPathConfiguration.AutoInstallVSCodeSupport,
                                                                     yarnPath.Value,
                                                                     ref typeScriptSdkVersion );
                        if( success )
                        {
                            Throw.DebugAssert( "The [NotNullWhen(true)] is ignored.", typeScriptSdkVersion != null );
                            CheckTypeScriptSdkVersion( monitor, typeScriptSdkVersion, typeScriptDep.Version.Base );
                        }
                    }
                }
                // Only try a compilation if no error occurred so far.
                if( success )
                {
                    success = YarnHelper.DoRunYarn( monitor, ckGenFolder, "run build", yarnPath.Value );
                }
                monitor.CloseGroup( success ? "Success." : "Failed." );
            }

            // Even if the build failed, if there is a targetPackageJson we configure its content.
            // We always ensure that the workspaces:["ck-gen"] and the "@local/ck-gen" dependency are here
            // and propagate the PeerDependencies from /ck-gen to the target project.
            // But if targetPackageJson is invalid, give up.
            if( targetPackageJson == null ) return false;

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
            var peerDependencies = generatedDependencies.Values.Where( d => d.DependencyKind is DependencyKind.PeerDependency ).ToList();
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
                
            if( BinPathConfiguration.EnsureTestSupport )
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
                        YarnHelper.EnsureSampleJestTestInSrcFolder( monitor, targetProjectPath );
                        YarnHelper.SetupJestConfigFile( monitor, targetProjectPath );
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
                            success &= YarnHelper.DoRunYarn( monitor, targetProjectPath, $"add --prefer-dev {toInstall.Concatenate( " " )}", yarnPath.Value );
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
                    success &= YarnHelper.DoRunYarn( monitor, targetProjectPath, "install", yarnPath.Value );
                }
            }
            return success;
        }

        static void CheckTypeScriptSdkVersion( IActivityMonitor monitor, SVersion typeScriptSdkVersion, SVersion targetTypeScriptVersion )
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
    }
}
