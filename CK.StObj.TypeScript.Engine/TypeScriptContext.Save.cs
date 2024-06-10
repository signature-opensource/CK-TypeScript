using CK.Core;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System;
using System.Linq;
using CK.TypeScript.CodeGen;

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
                var ckGenFolderSrc = ckGenFolder.AppendPart( "src" );

                var saver = new TypeScriptFileSaveStrategy( ckGenFolderSrc );
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
                        var targetPackageJson = YarnHelper.LoadPackageJson( monitor, projectJsonPath, out bool invalidPackageJson );

                        // Even if we don't know yet whether Yarn is installed, we lookup the Yarn typescript sdk version.
                        // Before compiling the ck-gen folder, we must ensure that the Yarn typescript sdk is installed: if it's not, package resolution fails miserably.
                        // We read the typeScriptSdkVersion here.
                        // We also return whether the target project has TypeScript installed: if yes, there's no need to "yarn add" it to
                        // the ck-gen project, "yarn install" is enough.
                        var targetTypeScriptVersion = FindBestTypeScriptVersion( monitor, targetProjectPath, targetPackageJson,
                                                                                 out var typeScriptSdkVersion,
                                                                                 out var targetProjectHasTypeScript );

                        // Generates "/ck-gen" files "package.json", "tsconfig.json" and "tsconfig-cjs.json".
                        // This may fail if there's an error in the dependencies declared by the code
                        // generator (in LibraryImport).
                        if( YarnHelper.SaveCKGenBuildConfig( monitor, ckGenFolder, targetTypeScriptVersion, this ) )
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
                                        // Ensuring that TypeScript is installed.
                                        if( !targetProjectHasTypeScript )
                                        {
                                            success &= YarnHelper.DoRunYarn( monitor, ckGenFolder, $"add --dev typescript@{targetTypeScriptVersion}", yarnPath.Value );
                                        }
                                        else
                                        {
                                            success &= YarnHelper.DoRunYarn( monitor, ckGenFolder, "install", yarnPath.Value );
                                        }
                                        if( typeScriptSdkVersion == null )
                                        {
                                            // Chicken & egg issue here:
                                            // "yarn sdks vscode" or "yarn sdks base" will not install typescript support unless "typescript" appears in the package.json
                                            // (and "yarn sdks tyescript" is not supported).
                                            // So we must ensure that when starting from scratch, the target package.json has typescript installed.
                                            // Starting from scratch: no error but no package.json in target project folder.
                                            if( !invalidPackageJson && targetPackageJson == null )
                                            {
                                                monitor.Info( $"Creating a minimal package.json with typescript development dependency '{targetTypeScriptVersion}'." );
                                                YarnHelper.WriteMinimalTargetProjectPackageJson( projectJsonPath, targetTypeScriptVersion );
                                            }

                                            monitor.Info( $"Yarn TypeScript sdk is not installed. Installing it with{(BinPathConfiguration.AutoInstallVSCodeSupport ? "" : "out")} VSCode support." );
                                            success &= YarnHelper.InstallYarnSdkSupport( monitor, targetProjectPath, BinPathConfiguration.AutoInstallVSCodeSupport, yarnPath.Value );
                                            if( success )
                                            {
                                                typeScriptSdkVersion = YarnHelper.GetYarnSdkTypeScriptVersion( monitor, targetProjectPath );
                                                if( typeScriptSdkVersion == null )
                                                {
                                                    monitor.Error( $"Unable to read back the TypeScript version used by the Yarn sdk." );
                                                    success = false;
                                                }
                                                else
                                                {
                                                    CheckTypeScriptSdkVersion( monitor, typeScriptSdkVersion, targetTypeScriptVersion );
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

                                    // If the lookup made previously to the target package.json is not on error, we handle EnsureTestSupport.
                                    // We do this even on compilation failure (if asked to do so) to be able to work in the
                                    // target project.

                                    // We always ensure that the workspaces:["ck-gen"] and "@local/ck-gen" dependency are here.
                                    // We may read again the typeScriptVersion here but we don't care and it unifies the behavior:
                                    // EnsureJestTestSupport "yarn add" only the packages that are not already here.
                                    if( !invalidPackageJson
                                        && YarnHelper.SetupTargetProjectPackageJson( monitor,
                                                                                     projectJsonPath,
                                                                                     targetPackageJson,
                                                                                     out var testScriptCommand,
                                                                                     out var typeScriptVersion,
                                                                                     out var jestVersion,
                                                                                     out var tsJestVersion,
                                                                                     out var typesJestVersion,
                                                                                     out var typesNodeVersion )
                                        && BinPathConfiguration.EnsureTestSupport )
                                    {
                                        // If we must ensure test support, we consider that as soon as a "test" script is available
                                        // we are done: the goal is to support "yarn test", Jest is our default test framework but is
                                        // not required.
                                        if( testScriptCommand != null && typeScriptVersion != null )
                                        {
                                            monitor.Info( $"TypeScript test script command '{testScriptCommand}' (and 'typescript@{typeScriptVersion}') already exists. " +
                                                          "Skipping EnsureJestTestSupport." );
                                        }
                                        else
                                        {
                                            // Note: Only the "typescript" package has a version here. 
                                            success &= EnsureJestTestSupport( monitor,
                                                                              targetProjectPath,
                                                                              projectJsonPath,
                                                                              testScriptCommand == null,
                                                                              typeScriptVersion, targetTypeScriptVersion,
                                                                              yarnPath.Value,
                                                                              jestVersion,
                                                                              tsJestVersion,
                                                                              typesJestVersion,
                                                                              typesNodeVersion );
                                        }
                                    }
                                }
                            }
                        }
                        else success = false;
                    }
                }
                else success = false;
            }
            return success;
        }

        static void CheckTypeScriptSdkVersion( IActivityMonitor monitor, string typeScriptSdkVersion, string targetTypeScriptVersion )
        {
            if( typeScriptSdkVersion != targetTypeScriptVersion )
            {
                monitor.Warn( $"The TypeScript version used by the Yarn sdk '{typeScriptSdkVersion}' differs from the selected one '{targetTypeScriptVersion}'.{Environment.NewLine}" +
                              $"This can lead to annoying issues such as import resolution failures." );
            }
        }

        string FindBestTypeScriptVersion( IActivityMonitor monitor,
                                          NormalizedPath targetProjectPath,
                                          JsonObject? targetPackageJson,
                                          out string? typeScriptSdkVersion,
                                          out bool targetProjectHasTypeScript )
        {
            targetProjectHasTypeScript = true;
            var typeScriptVersionSource = "target project";
            var targetTypeScriptVersion = targetPackageJson?["devDependencies"]?["typescript"]?.ToString();
            typeScriptSdkVersion = YarnHelper.GetYarnSdkTypeScriptVersion( monitor, targetProjectPath );
            if( targetTypeScriptVersion == null )
            {
                targetProjectHasTypeScript = false;
                if( typeScriptSdkVersion == null )
                {
                    typeScriptVersionSource = "BinPathConfiguration.AutomaticTypeScriptVersion property";
                    targetTypeScriptVersion = BinPathConfiguration.AutomaticTypeScriptVersion;
                }
                else
                {
                    typeScriptVersionSource = "Yarn TypeScript sdk";
                    targetTypeScriptVersion = typeScriptSdkVersion;
                }
            }
            monitor.Info( $"Considering TypeScript version '{targetTypeScriptVersion}' from {typeScriptVersionSource}." );
            if( typeScriptSdkVersion != null )
            {
                CheckTypeScriptSdkVersion( monitor, typeScriptSdkVersion, targetTypeScriptVersion );
            }
            return targetTypeScriptVersion;
        }

        static bool EnsureJestTestSupport( IActivityMonitor monitor,
                                           NormalizedPath targetProjectPath,
                                           NormalizedPath projectJsonPath,
                                           bool addTestJestScript,
                                           string? typeScriptVersion,
                                           string targetTypescriptVersion,
                                           NormalizedPath yarnPath,
                                           string? jestVersion,
                                           string? tsJestVersion,
                                           string? typesJestVersion,
                                           string? typesNodeVersion )
        {
            bool success = true;
            using( monitor.OpenInfo( $"Ensuring TypeScript test with Jest." ) )
            {
                string a = string.Empty, i = string.Empty;
                Add( ref a, ref i, "typescript", typeScriptVersion, targetTypescriptVersion );
                Add( ref a, ref i, "jest", jestVersion, null );
                Add( ref a, ref i, "ts-jest", tsJestVersion, null );
                Add( ref a, ref i, "@types/jest", typesJestVersion, null );
                Add( ref a, ref i, "@types/node", typesNodeVersion, null );

                static void Add( ref string a, ref string i, string name, string? currentVersion, string? version )
                {
                    if( currentVersion == null )
                    {
                        if( version != null ) name = $"{name}@{version}";
                        if( i.Length == 0 ) i = name;
                        else i += ' ' + name;
                    }
                    else
                    {
                        a = $"{a}{(a.Length == 0 ? "" : ", ")}{name}@{currentVersion}";
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
