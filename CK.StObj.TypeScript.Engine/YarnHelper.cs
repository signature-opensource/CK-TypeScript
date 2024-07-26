using CK.Core;
using CK.TypeScript.CodeGen;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace CK.Setup
{

    /// <summary>
    /// Provides helper for Node and Yarn.
    /// </summary>
    public static class YarnHelper
    {
        /// <summary>
        /// The Jest's setupFile name.
        /// </summary>
        public const string JestSetupFileName = "jest.CKTypeScriptEngine.ts";
        const string _testRunningKey = "CK_TYPESCRIPT_ENGINE";
        const string _yarnFileName = $"yarn-{TypeScriptAspectConfiguration.AutomaticYarnVersion}.cjs";
        const string _autoYarnPath = $".yarn/releases/{_yarnFileName}";

        static YarnHelper()
        {
            if( !typeof( YarnHelper ).Assembly.GetManifestResourceNames().Contains( $"CK.StObj.TypeScript.Engine.{_yarnFileName}" ) )
            {
                Throw.CKException( $"CK.StObj.TypeScript.Engine.csproj must be updated with <EmbeddedResource Include=\"../.yarn/releases/{_yarnFileName}\" />" );
            }
        }

        /// <summary>
        /// Locates yarn in <paramref name="workingDirectory"/> or above and calls it with the provided <paramref name="command"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="command">The command to run.</param>
        /// <param name="environmentVariables">Optional environment variables to set.</param>
        /// <returns>True on success, false if yarn cannot be found or the process failed.</returns>
        public static bool RunYarn( IActivityMonitor monitor, NormalizedPath workingDirectory, string command, Dictionary<string, string>? environmentVariables )
        {
            var yarnPath = TryFindYarn( workingDirectory, out var _ );
            if( yarnPath.HasValue )
            {
                return DoRunYarn( monitor, workingDirectory, command, yarnPath.Value, environmentVariables );
            }
            monitor.Error( $"Unable to find yarn in '{workingDirectory}' or above." );
            return false;
        }

        // Reads the typescript version from .yarn/sdks/typescript/package.json or returns null if the
        // Yarn TypeScript sdk is not installed or the version cannot be read.
        internal static SVersion? GetYarnSdkTypeScriptVersion( IActivityMonitor monitor, NormalizedPath targetProjectPath )
        {
            var sdkTypeScriptPath = targetProjectPath.Combine( ".yarn/sdks/typescript/package.json" );
            // We don't care of the ignoreVersionsBound (we'll never merge the versions).
            var packageJson = PackageJsonFile.ReadFile( monitor, sdkTypeScriptPath, ignoreVersionsBound: true );
            if( packageJson == null ) return null;
            if( packageJson.IsEmpty )
            {
                monitor.Warn( $"Missing expected '{sdkTypeScriptPath}' to be able to read the Yarn sdk TypeScript version." );
                return null;
            }
            if( packageJson.Version == null )
            {
                monitor.Warn( $"Missing version in '{sdkTypeScriptPath}'. Unable to read the Yarn sdk TypeScript version." );
                return null;
            }
            if( !packageJson.Version.Prerelease.Equals( "sdk", StringComparison.OrdinalIgnoreCase ) )
            {
                monitor.Error( $"Invalid Yarn sdks typescript version '{packageJson.Version}'. It should end with '-sdk'.{Environment.NewLine}File: '{sdkTypeScriptPath}'." );
                return null;
            }
            return SVersion.Create( packageJson.Version.Major, packageJson.Version.Minor, packageJson.Version.Patch );
        }

        /// <summary>
        /// Generates "/ck-gen/package.json", "/ck-gen/tsconfig.json" and potentially "/ck-gen/tsconfig-cjs.json" and "/ck-gen/tsconfig-es6.json".
        /// </summary>
        internal static bool SaveCKGenBuildConfig( IActivityMonitor monitor,
                                                   NormalizedPath ckGenFolder,
                                                   DependencyCollection deps,
                                                   TSModuleSystem moduleSystem,
                                                   bool enableTSProjectReferences,
                                                   string? filePrefix = null )
        {
            using var gLog = monitor.OpenInfo( $"Saving TypeScript and TypeScript configuration files..." );

            return GeneratePackageJson( monitor, ckGenFolder, moduleSystem, deps, filePrefix )
                   && GenerateTSConfigJson( monitor, ckGenFolder, moduleSystem, enableTSProjectReferences, filePrefix );

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

            static bool GenerateTSConfigJson( IActivityMonitor monitor, NormalizedPath ckGenFolder, TSModuleSystem moduleSystem, bool enableTSProjectReferences, string? filePrefix )
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

                    // Allow this project to be "composite" (this is currently badly supported by Jest).
                    var tsBuildMode = "";
                    if( enableTSProjectReferences )
                    {
                        tsBuildMode = """
                                             "composite": true,

                                      """;
                    }

                    File.WriteAllText( tsConfigFile, $$"""
                                                     {
                                                        "compilerOptions": {
                                                            "strict": true,
                                                            "target": "es2022",
                                                            "moduleResolution": "node",
                                                            "lib": ["es2022", "dom"],
                                                            "baseUrl": "./src",
                                                            "module": "{{module}}",
                                                            "outDir": "./dist/{{modulePath}}",
                                                            "sourceMap": true,
                                                            "declaration": true,
                                                            "declarationMap": true,
                                                            "esModuleInterop": true,
                                                            "resolveJsonModule": true,
                                                            {{tsBuildMode}}"rootDir": "src"
                                                        },
                                                        "include": [
                                                            "src/**/*"
                                                        ]
                                                     }
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

        internal static NormalizedPath? GetYarnInstallPath( IActivityMonitor monitor, NormalizedPath targetProjectPath, bool autoInstall )
        {
            var yarnPath = TryFindYarn( targetProjectPath, out var aboveCount );
            if( yarnPath.HasValue )
            {
                var current = yarnPath.Value.LastPart;
                if( current.StartsWith("yarn-")
                    && current.Length > 5
                    && Version.TryParse( Path.GetFileNameWithoutExtension( current.AsSpan( 5 ) ), out var version ) )
                {
                    if( version.Major < 4 )
                    {
                        monitor.Warn( $"Yarn found at '{yarnPath}' but expected version is Yarn 4. " +
                                      $"Please upgrade to Yarn 4: yarn set version {TypeScriptAspectConfiguration.AutomaticYarnVersion}." );

                    }
                    else
                    {
                        monitor.Info( $"Yarn {version.ToString(3)} found at '{yarnPath}'." );
                    }
                }
                else
                {
                    monitor.Warn( $"Unable to read version from Yarn found at '{yarnPath}'. Expected something like '{_yarnFileName}'." );
                }
            }
            else if( autoInstall )
            {
                var gitRoot = targetProjectPath.PathsToFirstPart( null, new[] { ".git" } ).FirstOrDefault( p => Directory.Exists( p ) );
                if( gitRoot.IsEmptyPath )
                {
                    monitor.Warn( $"No '.git' found above to setup a shared yarn. Auto installing yarn in target '{targetProjectPath}'." );
                    yarnPath = AutoInstall( monitor, targetProjectPath, 0 );
                }
                else
                {
                    Throw.DebugAssert( gitRoot.LastPart == ".git" );
                    monitor.Info( $"Git root found: '{gitRoot}'. Setting up a shared .yarn cache." );
                    aboveCount = targetProjectPath.Parts.Count - gitRoot.Parts.Count + 1;
                    yarnPath = AutoInstall( monitor, targetProjectPath, aboveCount );
                }
            }
            if( !yarnPath.HasValue )
            {
                monitor.Warn( $"No yarn found in '{targetProjectPath}' or above and AutoInstallYarn is false." );
            }
            else
            {
                EnsureYarnRcFileAtYarnLevel( monitor, yarnPath.Value );
            }
            return yarnPath;

            static NormalizedPath? AutoInstall( IActivityMonitor monitor, NormalizedPath targetProjectPath, int aboveCount )
            {
                NormalizedPath? yarnPath;
                var yarnRootPath = targetProjectPath.RemoveLastPart( aboveCount );
                monitor.Info( $"No yarn found, we will add our own {_autoYarnPath} in '{yarnRootPath}'." );
                var yarnBinDir = yarnRootPath.Combine( ".yarn/releases" );
                monitor.Trace( $"Extracting '{_yarnFileName}' to '{yarnBinDir}'." );
                Directory.CreateDirectory( yarnBinDir );
                yarnPath = yarnBinDir.AppendPart( _yarnFileName );
                var a = Assembly.GetExecutingAssembly();
                Throw.DebugAssert( a.GetName().Name == "CK.StObj.TypeScript.Engine" );
                using( var yarnBinStream = a.GetManifestResourceStream( $"CK.StObj.TypeScript.Engine.{_yarnFileName}" ) )
                using( var fileStream = File.OpenWrite( yarnPath ) )
                {
                    yarnBinStream!.CopyTo( fileStream );
                }
                HandleGitIgnore( monitor, yarnRootPath );
                return yarnPath;

                static void HandleGitIgnore( IActivityMonitor monitor, NormalizedPath yarnRootPath )
                {
                    var gitIgnore = yarnRootPath.AppendPart( ".gitignore" );
                    const string yarnDefault = """
                                  # Yarn - Not using Zero-Install (.yarn/cache and .pnp.* are not commited).
                                  .pnp.*
                                  .yarn/*
                                  !.yarn/patches
                                  !.yarn/plugins
                                  !.yarn/releases
                                  !.yarn/sdks
                                  !.yarn/versions

                                  # Because we can have subordinated .yarn folder we must exclude any .yarn/install-state.gz
                                  # and yarn/unplugged since we don't use Zero-Install.
                                  **/.yarn/install-state.gz
                                  **/.yarn/unplugged

                                  """;
                    if( File.Exists( gitIgnore ) )
                    {
                        var ignore = File.ReadAllText( gitIgnore );
                        if( !ignore.Contains( ".yarn/*" ) )
                        {
                            monitor.Info( $"No '.yarn/*' found in '{gitIgnore}'. Adding default section:{yarnDefault}" );
                            ignore += yarnDefault;
                        }
                        else
                        {
                            monitor.Info( $"At least '.yarn/*' found in '{gitIgnore}'. Skipping the injection of the default section:{yarnDefault}" );
                        }
                    }
                    else
                    {
                        monitor.Info( $"No '{gitIgnore}' found. Creating one with the default section:{yarnDefault}" );
                        File.WriteAllText( gitIgnore, yarnDefault );
                    }
                }
            }
        }

        static void EnsureYarnRcFileAtYarnLevel( IActivityMonitor monitor, NormalizedPath yarnPath )
        {
            Throw.DebugAssert( yarnPath.Parts.Count > 3 && yarnPath.Parts[^3] == ".yarn" && yarnPath.Parts[^2] == "releases" );
            var def = $"""
                       yarnPath: "./{yarnPath.RemoveFirstPart( yarnPath.Parts.Count - 3 )}"

                       # We don't use Zero Install: compression level defaults to 0 (no compression) in yarn 4
                       # because 0 (no compression) is slightly better for git. As we don't commit the packages,
                       # we continue to use the yarn 3 default compression mode.
                       compressionLevel: mixed

                       # We prevent Yarn to query the remote registries to validate that the lockfile
                       # content matches the remote information.
                       enableHardenedMode: false

                       # cacheFolder: "./.yarn/cache", enableGlobalCache: false and enableMirror: false
                       # Let each repository have its local cache, independent from any global cache.
                       cacheFolder: "./.yarn/cache"
                       enableGlobalCache: false
                       enableMirror: false

                       """;
            var yarnrcFile = yarnPath.RemoveLastPart( 3 ).AppendPart( ".yarnrc.yml" );
            if( File.Exists( yarnrcFile ) )
            {
                var current = File.ReadAllText( yarnrcFile );
                monitor.Info( $"File '{yarnrcFile}' exists, leaving it unchanged:{Environment.NewLine}{current}" );
            }
            else
            {
                monitor.Info( $"Creating '{yarnrcFile}':{Environment.NewLine}{def}" );
                File.WriteAllText( yarnrcFile, def );
            }
        }

        internal static bool HasVSCodeSupport( IActivityMonitor monitor, NormalizedPath targetProjectPath )
        {
            var integrationsFile = targetProjectPath.Combine( ".yarn/sdks/integrations.yml" );
            if( !File.Exists( integrationsFile ) ) return false;
            return File.ReadAllText( integrationsFile ).Contains( "- vscode" );
        }

        /// <summary>
        /// Executes "yarn add --dev @yarnpkg/sdks".
        /// TypeScript package MUST already be added for the TypeScript sdk to be installed.
        /// </summary>
        /// <param name="monitor">Required monitor.</param>
        /// <param name="targetProjectPath">The project path.</param>
        /// <param name="installVSCodeSupport">True to install the VSCode support.</param>
        /// <param name="yarnPath">The yarn path.</param>
        /// <param name="typeScriptSdkVersion">Updated sdk version that is read again.</param>
        /// <returns>True on success, false on error.</returns>
        internal static bool InstallYarnSdkSupport( IActivityMonitor monitor,
                                                    NormalizedPath targetProjectPath,
                                                    bool installVSCodeSupport,
                                                    NormalizedPath yarnPath,
                                                    [NotNullWhen(true)]ref SVersion? typeScriptSdkVersion )
        {
            if( DoRunYarn( monitor, targetProjectPath, "add --dev @yarnpkg/sdks", yarnPath )
                && DoRunYarn( monitor, targetProjectPath, installVSCodeSupport ? "sdks vscode" : "sdks base", yarnPath ) )
            {
                typeScriptSdkVersion = YarnHelper.GetYarnSdkTypeScriptVersion( monitor, targetProjectPath );
                if( typeScriptSdkVersion == null )
                {
                    monitor.Error( $"Unable to read back the TypeScript version used by the Yarn sdk." );
                    return false;
                }
                return true;
            }
            return false;
        }

        static NormalizedPath? TryFindYarn( NormalizedPath currentDirectory, out int aboveCount )
        {
            // Here we should find a .yarnrc.yml:
            //  - consider its yarnPath: "..." property.
            //  - return a YarnInfo that is a (YarnRCPath,YarnPath) tuple.
            //
            // var yarnRc = currentDirectory.PathsToFirstPart( null, new[] { ".yarnrc.yml" } ).FirstOrDefault( p => Directory.Exists( p ) );
            //
            // For the moment, we only handle .yarn/release/*js file.
            aboveCount = 0;
            while( currentDirectory.HasParts )
            {
                NormalizedPath releases = currentDirectory.Combine( ".yarn/releases" );
                if( Directory.Exists( releases ) )
                {
                    var yarn = Directory.GetFiles( releases )
                        .Select( s => Path.GetFileName( s ) )
                        // There is no dot on purpose, a js file can be js/mjs/cjs/whatever they invent next.
                        .Where( s => s.StartsWith( "yarn" ) && s.EndsWith( "js" ) )
                        .FirstOrDefault();
                    if( yarn != null ) return releases.AppendPart( yarn );
                }
                currentDirectory = currentDirectory.RemoveLastPart();
                aboveCount++;
            }
            return default;
        }

        internal static bool DoRunYarn( IActivityMonitor monitor,
                                        NormalizedPath workingDirectory,
                                        string command,
                                        NormalizedPath yarnPath,
                                        Dictionary<string, string>? environmentVariables = null )
        {
            using( monitor.OpenInfo( $"Running 'yarn {command}' in '{workingDirectory}'{(environmentVariables == null || environmentVariables.Count == 0
                                                                                            ? ""
                                                                                            : $" with {environmentVariables.Select( kv => $"'{kv.Key}': '{kv.Value}'" ).Concatenate()}")}." ) )
            {
                int code = RunProcess( monitor.ParallelLogger, "node", $"\"{yarnPath}\" {command}", workingDirectory, environmentVariables );
                if( code != 0 )
                {
                    monitor.Error( $"'yarn {command}' failed with code {code}." );
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Prepares the project to run Jest by setting the <see cref="JestSetupFileName"/> file with
        /// the provided <paramref name="environmentVariables"/> and (at least) the "CK_TYPESCRIPT_ENGINE = true".
        /// </summary>
        /// <param name="monitor">Required monitor.</param>
        /// <param name="targetProjectPath">The project path.</param>
        /// <param name="environmentVariables">Optional environment variables.</param>
        /// <param name="afterRun">
        /// A cleanup action that must be run once the test is over.
        /// This is null if the target package.json file has no "test" script or if the "test" is
        /// not "jest".
        /// </param>
        /// <returns>True on success, false on error.</returns>
        public static bool PrepareJestRun( IActivityMonitor monitor,
                                           NormalizedPath targetProjectPath,
                                           Dictionary<string, string>? environmentVariables,
                                           out Action? afterRun )
        {
            afterRun = null;
            var o = PackageJsonFile.ReadFile( monitor, targetProjectPath.AppendPart( "package.json" ), ignoreVersionsBound: true );
            if( o == null ) return false;
            if( o.Scripts.TryGetValue( "test", out var command ) && command == "jest" )
            {
                var jestSetupFilePath = targetProjectPath.AppendPart( JestSetupFileName );
                environmentVariables ??= new Dictionary<string, string>() { { _testRunningKey, "true" } };
                WriteJestSetupFile( jestSetupFilePath, environmentVariables );
                afterRun = () => WriteJestSetupFile( jestSetupFilePath, null );
            }
            return true;
        }

        internal static void SetupJestConfigFile( IActivityMonitor monitor, NormalizedPath targetProjectPath )
        {
            var jestConfigPath = targetProjectPath.AppendPart( "jest.config.js" );
            if( File.Exists( jestConfigPath ) )
            {
                var current = File.ReadAllText( jestConfigPath );
                if( current.Contains( "testEnvironment: 'node'," ) )
                {
                    current = current.Replace( "testEnvironment: 'node',", "testEnvironment: 'jsdom'," );
                    File.WriteAllText( jestConfigPath, current );
                    monitor.Warn( $"The 'jest.config.js' used testEnvironment: 'node', it has been changed to testEnvironment: 'jsdom'." );
                }
                if( current.Contains( "jest.StObjTypeScriptEngine.js" ) )
                {
                    current = current.Replace( "jest.StObjTypeScriptEngine.js", JestSetupFileName );
                    File.WriteAllText( jestConfigPath, current );
                    var previously = targetProjectPath.AppendPart( "jest.StObjTypeScriptEngine.js" );
                    if( File.Exists( previously ) ) File.Delete( previously );
                    monitor.Warn( $"Updated 'jest.config.js' setup file from 'jest.StObjTypeScriptEngine.js' to '{JestSetupFileName}'." );
                }
                else
                {
                    monitor.Trace( $"The 'jest.config.js' file exists and is up to date. Leaving it unchanged." );
                }
            }
            else
            {
                monitor.Info( $"Creating the 'jest.config.js' file (testEnvironment: 'jsdom')." );
                File.WriteAllText( jestConfigPath, $$$"""
                                                    // Jest is not ESM compliant. Using CJS here.
                                                    module.exports = {
                                                        moduleFileExtensions: ['js', 'json', 'ts'],
                                                        rootDir: 'src',
                                                        testRegex: '.*\\.spec\\.ts$',
                                                        transform: {
                                                            '^.+\\.ts$': ['ts-jest', {
                                                                // Removes annoying ts-jest[config] (WARN) message TS151001: If you have issues related to imports, you should consider...
                                                                diagnostics: {ignoreCodes: ['TS151001']}
                                                            }],
                                                        },
                                                        testEnvironment: 'jsdom',
                                                        setupFiles: ["../{{{JestSetupFileName}}}"]
                                                    };
                                                    """ );
            }
            // Always update the JestSetupFileName (jest.CKTypeScriptEngine.ts) so that we can change it
            // when we want.
            WriteJestSetupFile( targetProjectPath.AppendPart( JestSetupFileName ), null );
        }

        static void WriteJestSetupFile( NormalizedPath jestSetupFilePath, Dictionary<string,string>? environmentVariables )
        {
            Throw.DebugAssert( jestSetupFilePath.LastPart == JestSetupFileName );
            const string header = """
                                  // This will run once before each test file and before the testing framework is installed.
                                  // This is used by TestHelper.CreateTypeScriptTestRunner to duplicate environment variables settings
                                  // in a "persistent" way: these environment variables will be available until the Runner
                                  // returned by TestHelper.CreateTypeScriptTestRunner is disposed.
                                  //
                                  // This part fixes a bug in testEnvionment: 'jsdom':
                                  // <fix href="https://stackoverflow.com/questions/68468203/why-am-i-getting-textencoder-is-not-defined-in-jest">
                                  import { TextDecoder as ImportedTextDecoder, TextEncoder as ImportedTextEncoder, } from "util";
                                  Object.assign(global, { TextDecoder: ImportedTextDecoder, TextEncoder: ImportedTextEncoder, })
                                  // </fix>

                                  """;
            if( environmentVariables == null )
            {
                File.WriteAllText( jestSetupFilePath, header );
            }
            else
            {
                using var f = File.Create( jestSetupFilePath );
                using var text = new StreamWriter( f );
                text.WriteLine( header );
                text.Write( "Object.assign( process.env, " );
                text.Flush();
                using( var w = new Utf8JsonWriter( f ) )
                {
                    w.WriteStartObject();
                    bool hasTestKey = false;
                    foreach( var kv in environmentVariables )
                    {
                        hasTestKey |= kv.Key == _testRunningKey;
                        w.WriteString( kv.Key, kv.Value );
                    }
                    if( !hasTestKey )
                    {
                        w.WriteString( _testRunningKey, "true" );
                    }
                    w.WriteEndObject();
                    w.Flush();
                }
                text.WriteLine( ");" );
                text.Flush();
            }
        }

        internal static void EnsureSampleJestTestInSrcFolder( IActivityMonitor monitor, NormalizedPath targetProjectPath )
        {
            var srcFolder = targetProjectPath.AppendPart( "src" );
            Directory.CreateDirectory( srcFolder );
            var existingTestFile = Directory.EnumerateFiles( srcFolder, "*.spec.ts", SearchOption.AllDirectories ).FirstOrDefault();
            if( existingTestFile != null )
            {
                monitor.Info( $"At least a test file exists in 'src' folder: skipping 'src/sample.spec.ts' creation (found '{existingTestFile}')." );
                return;
            }
            else
            {
                var sampleTestPath = srcFolder.AppendPart( "sample.spec.ts" );
                monitor.Info( $"Creating 'src/sample.spec.ts' test file." );
                Directory.CreateDirectory( srcFolder );
                File.WriteAllText( sampleTestPath, """
                    // Trick from https://stackoverflow.com/a/77047461/190380
                    // When debugging ("Debug Test at Cursor" in menu), this cancels jest timeout.
                    if( process.env.VSCODE_INSPECTOR_OPTIONS ) jest.setTimeout(30 * 60 * 1000 ); // 30 minutes

                    // Sample test.
                    describe('Sample test', () => {
                        it('should be true', () => {
                          expect(true).toBeTruthy();
                        });
                      });
                    """ );
            }
        }

        #region ProcessRunner for NodeBuild

        static CKTrait StdErrTag = ActivityMonitor.Tags.Register( "StdErr" );
        static CKTrait StdOutTag = ActivityMonitor.Tags.Register( "StdOut" );

        static int RunProcess( IParallelLogger logger,
                               string fileName,
                               string arguments,
                               string workingDirectory,
                               Dictionary<string,string>? environmentVariables )
        {
            var info = new ProcessStartInfo( fileName, arguments )
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8
            };
            if( environmentVariables != null && environmentVariables.Count > 0 )
            {
                foreach( var kv in environmentVariables ) info.EnvironmentVariables.Add( kv.Key, kv.Value );
            }

            using var process = new Process { StartInfo = info };
            process.OutputDataReceived += ( sender, data ) =>
            {
                if( data.Data != null ) logger.Trace( StdOutTag, data.Data );
            };
            process.ErrorDataReceived += ( sender, data ) =>
            {
                if( data.Data != null ) logger.Trace( StdErrTag, data.Data );
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            return process.ExitCode;

        }

        #endregion
    }

}
