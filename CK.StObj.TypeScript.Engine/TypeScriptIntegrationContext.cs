using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Setup
{
    public sealed class TypeScriptIntegrationContext
    {
        readonly TypeScriptBinPathAspectConfiguration _configuration;
        readonly NormalizedPath _ckGenFolder;
        readonly PackageJsonFile _targetPackageJson;
        readonly TSConfigJsonFile _tsConfigJson;
        NormalizedPath _yarnPath;

        TypeScriptIntegrationContext( TypeScriptBinPathAspectConfiguration configuration,
                                      PackageJsonFile targetPackageJson,
                                      TSConfigJsonFile tSConfigJson )
        {
            _configuration = configuration;
            _ckGenFolder = configuration.TargetProjectPath.AppendPart( "ck-gen" );
            _targetPackageJson = targetPackageJson;
            _tsConfigJson = tSConfigJson;
        }

        public TypeScriptBinPathAspectConfiguration Configuration => _configuration;

        public NormalizedPath CKGenFolder => _ckGenFolder;

        public PackageJsonFile TargetPackageJson => _targetPackageJson;

        public TSConfigJsonFile TSConfigJson => _tsConfigJson;

        /// <summary>
        /// See <see cref="TypeScriptIntegrationContext.BeforeEventArgs"/>.
        /// </summary>
        public sealed class BeforeEventArgs : EventMonitoredArgs
        {
            readonly NormalizedPath _yarnPath;

            internal BeforeEventArgs( IActivityMonitor monitor,
                                      TypeScriptIntegrationContext integrationContext,
                                      SVersionBound typeScriptVersion,
                                      NormalizedPath yarnPath )
                : base( monitor ) 
            {
                IntegrationContext = integrationContext;
                TypeScriptVersion = typeScriptVersion;
                _yarnPath = yarnPath;
            }

            /// <summary>
            /// Gets the <see cref="TypeScriptIntegrationContext"/>.
            /// </summary>
            public TypeScriptIntegrationContext IntegrationContext { get; }


            public NormalizedPath TargetProjectPath => IntegrationContext.Configuration.TargetProjectPath;

            /// <summary>
            /// Gets the TypeScript version.
            /// </summary>
            public SVersionBound TypeScriptVersion { get; }

            /// <summary>
            /// Calls Yarn with the provided <paramref name="command"/>.
            /// </summary>
            /// <param name="monitor">The monitor to use.</param>
            /// <param name="command">The command to run.</param>
            /// <param name="environmentVariables">Optional environment variables to set.</param>
            /// <param name="workingDirectory">Optional working directory. Deadults to <see cref="TypeScriptBinPathAspectConfiguration.TargetProjectPath"/>.</param>
            /// <returns>True on success, false if the process failed.</returns>
            public bool RunYarn( IActivityMonitor monitor, string command, Dictionary<string, string>? environmentVariables = null, NormalizedPath workingDirectory = default )
            {
                if( workingDirectory.IsEmptyPath ) workingDirectory = IntegrationContext.Configuration.TargetProjectPath;
                return YarnHelper.DoRunYarn( monitor, workingDirectory, command, _yarnPath, environmentVariables );
            }

            public int RunProcess( IActivityMonitor monitor,
                                   string fileName,
                                   string arguments,
                                   Dictionary<string, string>? environmentVariables = null,
                                   NormalizedPath workingDirectory = default )
            {
                if( workingDirectory.IsEmptyPath ) workingDirectory = IntegrationContext.Configuration.TargetProjectPath;
                return YarnHelper.RunProcess( monitor.ParallelLogger, fileName, arguments, workingDirectory, environmentVariables );
            }
        }

        /// <summary>
        /// Raised before the integration step when <see cref="TypeScriptBinPathAspectConfiguration.IntegrationMode"/> is
        /// not <see cref="CKGenIntegrationMode.None"/>.
        /// <para>
        /// This event can be used to setup the target project before running.
        /// </para>
        /// </summary>
        public event EventHandler<BeforeEventArgs>? OnBeforeIntegration;

        internal static TypeScriptIntegrationContext? Create( IActivityMonitor monitor,
                                                              TypeScriptBinPathAspectConfiguration configuration )
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
            return new TypeScriptIntegrationContext( configuration, packageJson, tsConfigJson );
        }

        internal bool Run( IActivityMonitor monitor, TypeScriptFileSaveStrategy saver, PackageDependency typeScriptDep, SVersion? typeScriptSdkVersion )
        {
            var yarnPath = YarnHelper.GetYarnInstallPath( monitor,
                                                          _configuration.TargetProjectPath,
                                                          _configuration.AutoInstallYarn );
            if( !yarnPath.HasValue ) return false;
            _yarnPath = yarnPath.Value;
            var h = OnBeforeIntegration;
            if( OnBeforeIntegration != null )
            {
                var e = new BeforeEventArgs( monitor, this, typeScriptDep.Version, _yarnPath );
                OnBeforeIntegration( this, e );
            }
            switch( _configuration.IntegrationMode )
            {
                case CKGenIntegrationMode.NpmPackage:
                    return NpmPackageIntegrate( monitor, saver, typeScriptDep, typeScriptSdkVersion );
                case CKGenIntegrationMode.Inline:
                    return TSPathInlineIntegrate( monitor, saver, typeScriptDep, typeScriptSdkVersion );
                default: throw new NotImplementedException();
            }
        }

        bool NpmPackageIntegrate( IActivityMonitor monitor,
                                  TypeScriptFileSaveStrategy saver,
                                  PackageDependency typeScriptDep,
                                  SVersion? typeScriptSdkVersion )
        {
            // Generates "/ck-gen": "package.json", "tsconfig.json" and potentially "tsconfig-cjs.json" and "tsconfig-es6.json" files.
            // This may fail if there's an error in the dependencies declared by the code generator (in LibraryImport).
            if( !YarnHelper.SaveCKGenBuildConfig( monitor,
                                                  _ckGenFolder,
                                                  saver.GeneratedDependencies,
                                                  _configuration.ModuleSystem,
                                                  _configuration.UseSrcFolder,
                                                  _configuration.EnableTSProjectReferences ) )
            {
                return false;
            }

            // The workspace dependency.
            PackageDependency ckGenDep = new PackageDependency( "@local/ck-gen", SVersionBound.None, DependencyKind.DevDependency );

            // Chicken & egg issue here:
            // "yarn sdks vscode" or "yarn sdks base" will not install typescript support unless "typescript" appears in the package.json
            // (and "yarn sdks typescript" is not supported).
            // So we must ensure that when starting from scratch, the target package.json has typescript installed.
            if( _targetPackageJson.IsEmpty )
            {
                monitor.Info( $"Creating a minimal package.json with typescript development dependency '{typeScriptDep.Version.ToNpmString()}'." );
                _targetPackageJson.Dependencies.AddOrUpdate( monitor, typeScriptDep, cloneAddedDependency: false );
                _targetPackageJson.Name = _targetPackageJson.SafeName;
                _targetPackageJson.Private = true;
                _targetPackageJson.Workspaces.Add( "ck-gen" );
                _targetPackageJson.Dependencies.AddOrUpdate( monitor, ckGenDep, cloneAddedDependency: false );
                _targetPackageJson.Save();
            }

            bool success = true;
            // We have a yarn, we can build "@local/ck-gen".
            // If the targetPackageJson is empty, a minimal one is created but if it is on error, we
            // don't replace it!
            using( monitor.OpenInfo( $"Building '@local/ck-gen' package..." ) )
            {
                // Ensuring that TypeScript is installed in /ck-gen.
                // Install the /ck-gen dependencies.
                success &= YarnHelper.DoRunYarn( monitor, _ckGenFolder, "install", _yarnPath );
                // If the yarn type script sdk is not installed (target project level), we must install it before
                // trying to build the /ck-gen.
                if( typeScriptSdkVersion == null )
                {
                    using( monitor.OpenInfo( $"Yarn TypeScript sdk is not installed. Installing it with{(_configuration.AutoInstallVSCodeSupport ? "" : "out")} VSCode support." ) )
                    {
                        success &= YarnHelper.InstallYarnSdkSupport( monitor,
                                                                     _configuration.TargetProjectPath,
                                                                     _configuration.AutoInstallVSCodeSupport,
                                                                     _yarnPath,
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
            int changeTracker = _targetPackageJson.Dependencies.ChangeTracker;
            if( !_targetPackageJson.Workspaces.Contains( "*" ) && _targetPackageJson.Workspaces.Add( "ck-gen" ) )
            {
                shouldRunYarnInstall = true;
                monitor.Info( $"Added \"ck-gen\" workspace." );
            }
            if( !_targetPackageJson.Dependencies.TryGetValue( "@local/ck-gen", out var ck )
                || !ck.IsWorkspaceDependency
                || ck.DependencyKind != DependencyKind.DevDependency )
            {
                shouldRunYarnInstall = true;
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
            // Propagates the PeerDependencies from /ck-gen to the target project.
            var peerDependencies = saver.GeneratedDependencies.Values.Where( d => d.DependencyKind is DependencyKind.PeerDependency ).ToList();
            if( peerDependencies.Count > 0 )
            {
                using( monitor.OpenInfo( $"Propagating {peerDependencies.Count} peer dependencies from /ck-gen to target project." ) )
                {
                    // There is no reason for this to fail because we setup the targetPackageJson.Dependencies
                    // to ignore version bounds.
                    success &= _targetPackageJson.Dependencies.UpdateDependencies( monitor, peerDependencies );
                    shouldRunYarnInstall = changeTracker != _targetPackageJson.Dependencies.ChangeTracker;
                }
            }
            _targetPackageJson.Save();
            // Only try a compilation of the /ck-gen if no error occurred so far.
            if( success )
            {
                success = YarnHelper.DoRunYarn( monitor, _ckGenFolder, "run build", _yarnPath );
            }
            // If the tsConfig is empty (it doesn't exist), let's create a default one: a tsConfig.json is 
            // required by our jest.config.js (and it should almost always exist).
            // If the tsc --init fails, ignores and continue (there sould be no reason for this to fail as we checked
            // that no tsConfig.json already exists).
            EnsureTargetTSConfigJson( monitor );
            if( _configuration.EnsureTestSupport )
            {
                // If we must ensure test support, we consider that as soon as a "test" script is available
                // we are done: the goal is to support "yarn test", Jest is our default test framework but is
                // not required.
                PackageDependency? actualTypeScriptDep;
                if( _targetPackageJson.Dependencies.TryGetValue( "typescript", out actualTypeScriptDep )
                    && _targetPackageJson.Scripts.TryGetValue( "test", out var testCommand )
                    && testCommand != "jest" )
                {
                    monitor.Warn( $"TypeScript test script command '{testCommand}' is not 'jest'. Skipping Jest tests setup." );
                }
                else
                {
                    using( monitor.OpenInfo( $"Ensuring TypeScript test with Jest." ) )
                    {
                        // Always setup jest even on error.
                        YarnHelper.EnsureSampleJestTestInSrcFolder( monitor, _configuration.TargetProjectPath );
                        YarnHelper.SetupJestConfigFile( monitor, _configuration.TargetProjectPath );
                        _targetPackageJson.Scripts["test"] = "jest";
                        _targetPackageJson.Save();

                        int dCount = _targetPackageJson.Dependencies.Count;
                        IEnumerable<string> toInstall = new string[]
                        {
                                            "jest",
                                            "ts-jest",
                                            "@types/jest",
                                            "@types/node",
                                            // Because we use testEnvironment: 'jsdom' (this package is required from jest v29).
                                            "jest-environment-jsdom"
                        };
                        toInstall = toInstall.Where( p => !_targetPackageJson.Dependencies.ContainsKey( p ) );
                        // Don't touch the exisiting typescript.
                        if( actualTypeScriptDep == null )
                        {
                            toInstall = toInstall.Append( $"typescript@{typeScriptDep.Version.ToNpmString()}" );
                        }
                        if( toInstall.Any() )
                        {
                            success &= YarnHelper.DoRunYarn( monitor, _configuration.TargetProjectPath, $"add --prefer-dev {toInstall.Concatenate( " " )}", _yarnPath );
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
                    success &= YarnHelper.DoRunYarn( monitor, _configuration.TargetProjectPath, "install", _yarnPath );
                }
            }
            return success;
        }

        bool TSPathInlineIntegrate( IActivityMonitor monitor,
                                    TypeScriptFileSaveStrategy saver,
                                    PackageDependency typeScriptDep,
                                    SVersion? typeScriptSdkVersion )
        {
            // If this fails, we don't care: this is purely informational.
            YarnHelper.SaveCKGenBuildConfig( monitor,
                                                _ckGenFolder,
                                                saver.GeneratedDependencies,
                                                _configuration.ModuleSystem,
                                                _configuration.UseSrcFolder,
                                                _configuration.EnableTSProjectReferences,
                                                filePrefix: "CouldBe." );
            // Chicken & egg issue here:
            // "yarn sdks vscode" or "yarn sdks base" will not install typescript support unless "typescript" appears in the package.json
            // (and "yarn sdks typescript" is not supported).
            // So we must ensure that when starting from scratch, the target package.json has typescript installed.
            bool success = true;
            if( _targetPackageJson.IsEmpty )
            {
                monitor.Info( $"Creating a minimal package.json with typescript development dependency '{typeScriptDep.Version.ToNpmString()}'." );
                _targetPackageJson.Dependencies.AddOrUpdate( monitor, typeScriptDep, cloneAddedDependency: false );
                _targetPackageJson.Name = _targetPackageJson.SafeName;
                _targetPackageJson.Private = true;
                _targetPackageJson.Save();
                // Ensuring that TypeScript is installed.
                success &= YarnHelper.DoRunYarn( monitor, _configuration.TargetProjectPath, "install", _yarnPath );
            }
            // If the yarn type script sdk is not installed (target project level), we must install it before
            // trying to build anything.
            if( typeScriptSdkVersion == null )
            {
                using( monitor.OpenInfo( $"Yarn TypeScript sdk is not installed. Installing it with{(_configuration.AutoInstallVSCodeSupport ? "" : "out")} VSCode support." ) )
                {
                    success &= YarnHelper.InstallYarnSdkSupport( monitor,
                                                                 _configuration.TargetProjectPath,
                                                                 _configuration.AutoInstallVSCodeSupport,
                                                                 _yarnPath,
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
            if( _targetPackageJson.Dependencies.Remove( "@local/ck-gen" ) )
            {
                monitor.Info( "Removed '@local/ck-gen' package dependency (NpmPackage integration mode)." );
                shouldRunYarnInstall = true;
            }
            if( _targetPackageJson.Workspaces.Remove( "ck-gen" ) )
            {
                monitor.Info( $"Removed 'ck-gen' Yarn workspace (NpmPackage integration mode)." );
                shouldRunYarnInstall = true;
            }
            // Propagates the dependencies required by generated code to the target project.
            int changeTracker = _targetPackageJson.Dependencies.ChangeTracker;
            using( monitor.OpenInfo( $"Updating {saver.GeneratedDependencies.Count} dependencies." ) )
            {
                // There is no reason for this to fail because we setup the targetPackageJson.Dependencies
                // to ignore version bounds.
                success &= _targetPackageJson.Dependencies.UpdateDependencies( monitor, saver.GeneratedDependencies.Values );
                shouldRunYarnInstall = changeTracker != _targetPackageJson.Dependencies.ChangeTracker;
            }
            _targetPackageJson.Save();
            if( shouldRunYarnInstall )
            {
                success &= YarnHelper.DoRunYarn( monitor, _configuration.TargetProjectPath, "install", _yarnPath );
            }

            // If the tsConfig is empty (it doesn't exist), let's create a default one.
            // If the tsc --init fails, ignores and continue (there sould be no reason for this to fail as we checked
            // that no tsConfig.json already exists).
            EnsureTargetTSConfigJson( monitor );

            // Ensure that the compilerOptions:paths has entries:
            //   "@local/ck-gen": ["./ck-gen/src"]
            //   "@local/ck-gen/*": ["./ck-gen/src/*"]
            if( !_tsConfigJson.ResolvedBaseUrl.TryGetRelativePathTo( _configuration.TargetCKGenPath, out NormalizedPath mapping ) )
            {
                monitor.Error( $"Unable to compute relative path from tsConfig.json baseUrl '{_tsConfigJson.ResolvedBaseUrl}' to {_configuration.TargetCKGenPath}." );
                return false;
            }
            bool shouldSaveTSConfig = false;
            string to = "./" + mapping;
            // Only one | here! (We want to do both.)
            if( _tsConfigJson.CompileOptionsPathEnsureMapping( "@local/ck-gen", to ) | _tsConfigJson.CompileOptionsPathEnsureMapping( "@local/ck-gen/*", to + "/*" ) )
            {
                monitor.Info( $"CompilerOption Paths mapped \"@local/ck-gen\" to \"{mapping}\"." );
                shouldSaveTSConfig = true;
            }
            if( shouldSaveTSConfig )
            {
                _tsConfigJson.Save();
            }

            if( _configuration.EnsureTestSupport )
            {
                // If we must ensure test support, we consider that as soon as a "test" script is available
                // we are done: the goal is to support "yarn test", Jest is our default test framework but is
                // not required.
                if( _targetPackageJson.Dependencies.TryGetValue( "typescript", out PackageDependency? actualTypeScriptDep )
                    && _targetPackageJson.Scripts.TryGetValue( "test", out var testCommand )
                    && testCommand != "jest" )
                {
                    monitor.Warn( $"TypeScript test script command '{testCommand}' is not 'jest'. Skipping Jest tests setup." );
                }
                else
                {
                    using( monitor.OpenInfo( $"Ensuring TypeScript test with Jest." ) )
                    {
                        // Always setup jest even on error.
                        YarnHelper.EnsureSampleJestTestInSrcFolder( monitor, _configuration.TargetProjectPath );
                        YarnHelper.SetupJestConfigFile( monitor, _configuration.TargetProjectPath );
                        _targetPackageJson.Scripts["test"] = "jest";
                        _targetPackageJson.Save();

                        int dCount = _targetPackageJson.Dependencies.Count;
                        IEnumerable<string> toInstall =
                        [
                                            "jest",
                                            "ts-jest",
                                            "@types/jest",
                                            "@types/node",
                                            // Because we use testEnvironment: 'jsdom' (this package is required by jest v29+).
                                            "jest-environment-jsdom"
                        ];
                        toInstall = toInstall.Where( p => !_targetPackageJson.Dependencies.ContainsKey( p ) );
                        // Don't touch the exisitng typescript.
                        if( actualTypeScriptDep == null )
                        {
                            toInstall = toInstall.Append( $"typescript@{typeScriptDep.Version.ToNpmString()}" );
                        }
                        if( toInstall.Any() )
                        {
                            success &= YarnHelper.DoRunYarn( monitor, _configuration.TargetProjectPath, $"add --prefer-dev {toInstall.Concatenate( " " )}", _yarnPath );
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
                    success &= YarnHelper.DoRunYarn( monitor, _configuration.TargetProjectPath, "install", _yarnPath );
                }
            }
            return success;
        }

        bool EnsureTargetTSConfigJson( IActivityMonitor monitor )
        {
            if( _tsConfigJson.IsEmpty )
            {
                // No need to log: The "tsc --init" will appear in the logs. 
                if( YarnHelper.DoRunYarn( monitor, _configuration.TargetProjectPath, "tsc --init", _yarnPath ) )
                {
                    // Read it back...
                    if( !_tsConfigJson.Reload( monitor ) )
                    {
                        return false;
                    }
                    // ... and save it again: this removes the comments and you know what?
                    // jest v29 fails when a comment appears in a tsConfig.json!!! 
                    _tsConfigJson.Save();
                }
            }
            return true;
        }

        internal static void WarnDiffTypeScriptSdkVersion( IActivityMonitor monitor, SVersion typeScriptSdkVersion, SVersion targetTypeScriptVersion )
        {
            if( typeScriptSdkVersion != targetTypeScriptVersion )
            {
                monitor.Warn( $"The TypeScript version used by the Yarn sdk '{typeScriptSdkVersion}' differs from the selected one '{targetTypeScriptVersion}'.{Environment.NewLine}" +
                              $"This can lead to annoying issues such as import resolution failures." );
            }
        }

    }
}
