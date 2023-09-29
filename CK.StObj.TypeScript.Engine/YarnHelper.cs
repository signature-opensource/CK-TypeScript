using CK.Core;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CK.Setup
{
    /// <summary>
    /// Provides helper for Yarn.
    /// </summary>
    public static class YarnHelper
    {
        const string _yarnFileName = $"yarn-{TypeScriptAspectBinPathConfiguration.AutomaticYarnVersion}.cjs";
        const string _autoYarnPath = $".yarn/releases/{_yarnFileName}";

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

        /// <summary>
        /// Generates "package.json", "tsconfig.json" and "tsconfig-cjs.json".
        /// </summary>
        internal static bool SaveCKGenBuildConfig( IActivityMonitor monitor, NormalizedPath outputPath, string? targetTypescriptVersion, TypeScriptContext g )
        {
            using var gLog = monitor.OpenInfo( $"Saving TypeScript and Yarn build configuration files..." );

            var reusable = new StringBuilder();
            return GeneratePackageJson( monitor, outputPath, targetTypescriptVersion, g, reusable )
                   && GenerateTSConfigJson( monitor, outputPath, g, reusable )
                   && GenerateTSConfigCJSJson( monitor, outputPath, g, reusable );

            static bool GeneratePackageJson( IActivityMonitor monitor, NormalizedPath outputPath, string? targetTypescriptVersion, TypeScriptContext g, StringBuilder sb )
            {
                sb.Clear();
                var packageJsonPath = Path.Combine( outputPath, "package.json" );
                using( monitor.OpenTrace( $"Creating '{packageJsonPath}'." ) )
                {
                    bool success = true;
                    var dependencies = new Dictionary<string, LibraryImport>();
                    if( targetTypescriptVersion != null )
                    {
                        dependencies.Add( "typescript", new LibraryImport( "typescript", targetTypescriptVersion, DependencyKind.DevDependency ) );
                    }
                    foreach( var file in g.Root.AllFilesRecursive )
                    {
                        foreach( var item in file.Imports.LibraryImports.Values )
                        {
                            if( !dependencies.TryGetValue( item.Name, out var prevImport ) )
                            {
                                dependencies.Add( item.Name, item );
                            }
                            else
                            {
                                if( item.Version != prevImport.Version )
                                {
                                    monitor.Error( $"File {file.Name} require {item.Name} at version {item.Version}, but another file require it at version {item.Version}." );
                                    success = false;
                                }
                                if( item.DependencyKind > prevImport.DependencyKind )
                                {
                                    monitor.Info( $"Dependency {item.Name} had dependency kind {prevImport.DependencyKind}, {file.Name} is upgrading it to {item.DependencyKind}" );
                                    dependencies[item.Name] = item;
                                }
                            }
                        }
                    }
                    if( !success ) return false;


                    sb.Clear();
                    sb.Append( """
                               {
                                  "name": "@local/ck-gen",
                               """ );
                    var depsList = dependencies
                        .Concat( dependencies.Where( s => s.Value.DependencyKind == DependencyKind.PeerDependency )
                                             .Select( s => KeyValuePair.Create( s.Key,
                                                                                new LibraryImport( s.Value.Name, s.Value.Version, DependencyKind.DevDependency ) ) ) )
                        .GroupBy( s => s.Value.DependencyKind, s => $@"    ""{s.Value.Name}"":""{s.Value.Version}""" )
                        .Select( s => (s.Key switch
                        {
                            DependencyKind.Dependency => "dependencies",
                            DependencyKind.DevDependency => "devDependencies",
                            DependencyKind.PeerDependency => "peerDependencies",
                            _ => Throw.InvalidOperationException<string>()
                        }, string.Join( ",\n", s )) );

                    foreach( var deps in depsList )
                    {
                        sb.Append( "  \"" ).Append( deps.Item1 ).Append( "\":{" ).Append( deps.Item2 ).AppendLine().Append( "}," );
                    }
                    sb.Append( """
                                 "private": true,
                                  "files": [
                                    "dist/"
                                  ],
                                  "main": "./dist/cjs/index.js",
                                  "module": "./dist/esm/index.js",
                                  "scripts": {
                                    "build": "tsc -p tsconfig.json && tsc -p tsconfig-cjs.json"
                                  }
                                }
                                """ );
                    File.WriteAllText( packageJsonPath, sb.ToString() );
                    return true;
                }
            }

            static bool GenerateTSConfigJson( IActivityMonitor monitor, NormalizedPath outputPath, TypeScriptContext g, StringBuilder sb )
            {
                sb.Clear();
                var tsConfigFile = Path.Combine( outputPath, "tsconfig.json" );
                using( monitor.OpenTrace( $"Creating '{tsConfigFile}'." ) )
                {
                    File.WriteAllText( tsConfigFile, """
                                                     {
                                                        "compilerOptions": {
                                                            "strict": true,
                                                            "target": "es5",
                                                            "module": "ES6",
                                                            "moduleResolution": "node",
                                                            "lib": ["es2015", "es2016", "es2017", "dom"],
                                                            "baseUrl": "./src",
                                                            "outDir": "./dist/esm",
                                                            "sourceMap": true,
                                                            "declaration": true,
                                                            "esModuleInterop": true,
                                                            "resolveJsonModule": true,
                                                            "rootDir": "src"
                                                        },
                                                        "include": [
                                                            "src/**/*"
                                                        ]
                                                     }
                                                     """ );
                }
                return true;
            }

            static bool GenerateTSConfigCJSJson( IActivityMonitor monitor, NormalizedPath outputPath, TypeScriptRoot root, StringBuilder sb )
            {
                sb.Clear();
                var tsConfigCJSFile = Path.Combine( outputPath, "tsconfig-cjs.json" );
                monitor.Trace( $"Creating '{tsConfigCJSFile}'." );
                File.WriteAllText( tsConfigCJSFile, """
                                                    {
                                                      "extends": "./tsconfig.json",
                                                      "compilerOptions": {
                                                        "module": "CommonJS",
                                                        "outDir": "./dist/cjs"
                                                      },
                                                    }
                                                    """ );
                return true;
            }

        }

        internal static NormalizedPath? GetYarnInstallPath( IActivityMonitor monitor, NormalizedPath targetProjectPath, bool autoInstall )
        {
            var yarnPath = TryFindYarn( targetProjectPath, out var aboveCount );
            if( yarnPath.HasValue )
            {
                monitor.Info( $"Yarn found at '{yarnPath}'." );
            }
            else if( autoInstall )
            {
                var gitRoot = targetProjectPath.PathsToFirstPart( null, new[] { ".git" } ).FirstOrDefault( p => Directory.Exists( p ) );
                if( gitRoot.IsEmptyPath )
                {
                    monitor.Info( $"No '.git' found above to setup a shared yarn. Auto installing yarn in target '{targetProjectPath}'." );
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
                return yarnPath;
            }
        }

        static void EnsureYarnRcFileAtYarnLevel( IActivityMonitor monitor, NormalizedPath yarnPath )
        {
            Throw.DebugAssert( yarnPath.Parts.Count > 3 && yarnPath.Parts[^3] == ".yarn" && yarnPath.Parts[^2] == "releases" );
            var yarnrcFile = yarnPath.RemoveLastPart( 3 ).AppendPart( ".yarnrc.yml" );
            if( File.Exists( yarnrcFile ) )
            {
                var content = File.ReadAllText( yarnrcFile );
                monitor.Trace( $"File '{yarnrcFile}' exists, leaving it unchanged:{Environment.NewLine}{content}" );
            }
            else
            {
                var content = $"""
                              yarnPath: "./{yarnPath.RemoveFirstPart(yarnPath.Parts.Count - 3)}"
                              enableGlobalCache: false
                              cacheFolder: "./.yarn/cache"
                              """;
                monitor.Info( $"Creating '{yarnrcFile}':{Environment.NewLine}{content}" );
                File.WriteAllText( yarnrcFile, content );
            }
        }

        internal static bool HasVSCodeSupport( IActivityMonitor monitor, NormalizedPath targetProjectPath )
        {
            return Directory.Exists( targetProjectPath.AppendPart( ".vscode" ) )
                   && Directory.Exists( targetProjectPath.Combine( ".yarn/sdks" ) );
        }

        internal static bool InstallVSCodeSupport( IActivityMonitor monitor, NormalizedPath targetProjectPath, NormalizedPath yarnPath )
        {
            return DoRunYarn( monitor, targetProjectPath, "add --dev @yarnpkg/sdks", yarnPath )
                   && DoRunYarn( monitor, targetProjectPath, "sdks vscode", yarnPath );
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

        internal static bool SetupTargetProjectPackageJson( IActivityMonitor monitor,
                                                            NormalizedPath projectJsonPath,
                                                            JsonObject? packageJson,
                                                            out string? testScriptCommand,
                                                            out string? jestVersion,
                                                            out string? tsJestVersion,
                                                            out string? typesJestVersion )
        {
            Throw.DebugAssert( projectJsonPath.LastPart == "package.json" );
            testScriptCommand = null;
            jestVersion = null;
            tsJestVersion = null;
            typesJestVersion = null;

            if( packageJson == null )
            {
                monitor.Info( $"Creating a minimal '{projectJsonPath}' without typescript development dependency." );
                WriteMinimalPackageJson( projectJsonPath, projectJsonPath );
                return true;
            }
            bool modified = false;
            if( !EnsureCKGenWorkspace( monitor, packageJson, ref modified )
                || !EnsureCKGenPackage( monitor, packageJson, ref modified ) )
            {
                monitor.Error( $"Error in '{projectJsonPath}'. Skipping ck-gen workspace and package configuration." );
                return false;
            }
            testScriptCommand = packageJson["scripts"]?["test"]?.ToString();
            JsonObject? devDependencies = packageJson["devDependencies"] as JsonObject;
            if( devDependencies != null )
            {
                jestVersion = devDependencies["jest"]?.ToString();
                tsJestVersion = devDependencies["ts-jest"]?.ToString();
                typesJestVersion = devDependencies["@types/jest"]?.ToString();
            }
            return !modified || SavePackageJsonFile( monitor, projectJsonPath, packageJson );

            static void WriteMinimalPackageJson( NormalizedPath targetProjectPath, NormalizedPath projectJsonPath )
            {
                File.WriteAllText( projectJsonPath,
                   $$"""
                    {
                        "name": "{{targetProjectPath.Parts[^2].ToLowerInvariant()}}",
                        "private": true,
                        "workspaces":["ck-gen"],
                        "dependencies": {
                            "@local/ck-gen": "workspace:*"
                        }
                    }
                    """ );
            }

            static bool EnsureCKGenWorkspace( IActivityMonitor monitor, JsonObject o, ref bool modified )
            {
                var workspaces = o["workspaces"] as JsonArray;
                if( workspaces == null )
                {
                    if( o["workspaces"] != null )
                    {
                        monitor.Error( $"\"workspaces\" property is not an array." );
                        return false;
                    }
                    o.Add( "workspaces", new JsonArray( "ck-gen" ) );
                }
                else
                {
                    if( workspaces.Any( x => x is JsonValue v && v.TryGetValue<string>( out var s ) && s == "ck-gen" ) )
                    {
                        return true;
                    }
                    workspaces.Add( "ck-gen" );
                }
                modified = true;
                return true;
            }

            static bool EnsureCKGenPackage( IActivityMonitor monitor, JsonObject o, ref bool modified )
            {
                var dependencies = EnsureJsonObject( monitor, o, "dependencies", ref modified );
                if( dependencies == null ) return false;
                if( dependencies["@local/ck-gen"] is JsonValue v && v.TryGetValue<string>( out var d ) && d == "workspace:*" )
                {
                    return true;
                }
                modified = true;
                dependencies["@local/ck-gen"] = JsonValue.Create( "workspace:*" );
                return true;
            }
        }

        internal static JsonObject? EnsureJsonObject( IActivityMonitor monitor, JsonObject parent, string name, ref bool modified )
        {
            var sN = parent[name];
            var sub = sN as JsonObject;
            if( sub == null )
            {
                if( sN != null )
                {
                    monitor.Error( $"\"{name}\" property is not an object." );
                    return null;
                }
                modified = true;
                parent.Add( name, sub = new JsonObject() );
            }
            return sub;
        }

        internal static JsonObject? LoadPackageJson( IActivityMonitor monitor, NormalizedPath projectJsonPath, out bool invalidPackageJson )
        {
            invalidPackageJson = false;
            try
            {
                if( !File.Exists( projectJsonPath ) ) return null;
                using var f = File.OpenRead( projectJsonPath );
                var doc = JsonNode.Parse( f,
                                          nodeOptions: null,
                                          new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip } );

                var o = doc as JsonObject;
                if( o == null )
                {
                    invalidPackageJson = true;
                    monitor.Error( $"File 'package.json' doesn't contain a Json object." );
                }
                return o;
            }
            catch( Exception ex )
            {
                monitor.Error( $"Unable to read file '{projectJsonPath}' file.", ex );
                invalidPackageJson = true;
                return null;
            }
        }

        internal static bool SavePackageJsonFile( IActivityMonitor monitor, NormalizedPath projectJsonPath, JsonObject o )
        {
            using( monitor.OpenInfo( $"Updating '{projectJsonPath}'." ) )
            {
                try
                {
                    // File.Create must be used to Truncate the file!
                    using var fOut = File.Create( projectJsonPath );
                    using var wOut = new Utf8JsonWriter( fOut, new JsonWriterOptions { Indented = true } );
                    o.WriteTo( wOut );
                    return true;
                }
                catch( Exception ex )
                {
                    monitor.Error( $"While writing '{projectJsonPath}'.", ex );
                    return false;
                }
            }
        }

        internal static bool DoRunYarn( IActivityMonitor monitor,
                                        NormalizedPath workingDirectory,
                                        string command,
                                        NormalizedPath yarnPath,
                                        Dictionary<string, string>? environmentVariables = null )
        {
            using( monitor.OpenInfo( $"Running 'yarn {command}' in '{workingDirectory}'{(environmentVariables == null || environmentVariables.Count == 0
                                                                                            ? ""
                                                                                            : $"with {environmentVariables.Select( kv => $"'{kv.Key}': '{kv.Value}'" ).Concatenate()}.")}." ) )
            {
                int code = RunProcess( monitor, "node", $"{yarnPath} {command}", workingDirectory, environmentVariables );
                if( code != 0 )
                {
                    monitor.Error( $"'yarn {command}' failed with code {code}." );
                    return false;
                }
            }
            return true;
        }

        internal static void SetupJestConfigFile( IActivityMonitor monitor, NormalizedPath targetProjectPath )
        {
            var jestConfigPath = targetProjectPath.AppendPart( "jest.config.js" );
            if( File.Exists( jestConfigPath ) )
            {
                monitor.Info( $"The 'jest.config.js' file exists: leaving it unchanged." );
            }
            else
            {
                monitor.Info( $"Creating the 'jest.config.js' file." );
                File.WriteAllText( jestConfigPath, """
                                                    module.exports = {
                                                        moduleFileExtensions: ['js', 'json', 'ts'],
                                                        rootDir: 'src',
                                                        testRegex: '.*\\.spec\\.ts$',
                                                        transform: {
                                                        '^.+\\.(t|j)s$': 'ts-jest',
                                                        },
                                                        testEnvironment: 'node',
                                                    };
                                                    """ );
            }
        }

        internal static void EnsureSampleJestTestInSrcFolder( IActivityMonitor monitor, NormalizedPath targetProjectPath )
        {
            var srcFolder = targetProjectPath.AppendPart( "src" );
            Directory.CreateDirectory( srcFolder );
            var existingTestFile = Directory.EnumerateFiles( srcFolder, "*.spec.ts", SearchOption.AllDirectories ).FirstOrDefault();
            if( existingTestFile != null )
            {
                monitor.Info( $"At least a test file exists in 'src' folder: skipping 'src/sample.spec.ts' creation ({existingTestFile})." );
                return;
            }
            else
            {
                var sampleTestPath = srcFolder.AppendPart( "sample.spec.ts" );
                monitor.Info( $"Creating 'src/sample.spec.ts' test file." );
                Directory.CreateDirectory( srcFolder );
                File.WriteAllText( sampleTestPath,
                    """
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

        static int RunProcess( IActivityMonitor monitor,
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
            process.Start();
            var left = new ChannelTextReader( process.StandardOutput );
            var right = new ChannelTextReader( process.StandardError );
            var processLogs = new ChannelReaderMerger<string, string, (string, bool)>(
                left, ( s ) => (s, false),
                right, ( s ) => (s, true)
            );
            _ = left.StartAsync();
            _ = right.StartAsync();
            _ = processLogs.StartAsync();

            bool firstLoop = true;
            while( !processLogs.Completion.IsCompleted || !process.HasExited )
            {
                FlushLogs();
                if( !firstLoop ) process.WaitForExit( 20 ); // avoid closed loop when waiting for log.
                firstLoop = false;
            }
            FlushLogs();
            Debug.Assert( process.HasExited );

            return process.ExitCode;

            void FlushLogs()
            {
                while( processLogs.TryRead( out var log ) )
                {
                    monitor.Log( log.Item2 ? LogLevel.Error : LogLevel.Trace, log.Item1 );
                }
            }

        }

        abstract class ChannelReaderWrapper<T> : ChannelReader<T>
        {
            readonly Channel<T> _channel;
            [AllowNull] Task _completion;

            protected ChannelReaderWrapper( Channel<T> channel )
            {
                _channel = channel;
            }

            public Task StartAsync() => _completion = BackgroundTaskAsync();

            protected ChannelWriter<T> Writer => _channel.Writer;

            /// <inheritdoc/>
            public override Task Completion => _completion;

            /// <inheritdoc/>
            public override bool TryRead( [MaybeNullWhen( false )] out T item ) => _channel.Reader.TryRead( out item );

            /// <inheritdoc/>
            public override ValueTask<T> ReadAsync( CancellationToken cancellationToken = default )
                => _channel.Reader.ReadAsync( cancellationToken );

            /// <inheritdoc/>
            public override bool TryPeek( [MaybeNullWhen( false )] out T item )
                => _channel.Reader.TryPeek( out item );

            /// <inheritdoc/>
            public override ValueTask<bool> WaitToReadAsync( CancellationToken cancellationToken = default ) => _channel.Reader.WaitToReadAsync( cancellationToken );

            protected abstract Task BackgroundTaskAsync();
        }

        sealed class ChannelTextReader : ChannelReaderWrapper<string>
        {
            readonly TextReader _textReader;

            public ChannelTextReader( TextReader textReader )
                : base( Channel.CreateUnbounded<string>() )
            {
                Throw.CheckNotNullArgument( textReader );
                _textReader = textReader;
            }

            protected override async Task BackgroundTaskAsync()
            {
                while( true )
                {
                    var line = await _textReader.ReadLineAsync();
                    if( line == null ) break;
                    Writer.TryWrite( line );
                }
                Writer.Complete();
            }
        }

        sealed class ChannelReaderMerger<TLeft, TRight, TOut> : ChannelReaderWrapper<TOut>
        {
            readonly ChannelReader<TLeft> _left;
            readonly Func<TLeft, TOut> _leftTransformer;
            readonly ChannelReader<TRight> _right;
            readonly Func<TRight, TOut> _rightTransformer;

            public ChannelReaderMerger( ChannelReader<TLeft> left,
                                        Func<TLeft, TOut> leftTransformer,
                                        ChannelReader<TRight> right,
                                        Func<TRight, TOut> rightTransformer,
                                        bool singleReader = true )
                : base( Channel.CreateUnbounded<TOut>( new UnboundedChannelOptions()
                {
                    SingleWriter = false,
                    SingleReader = singleReader
                } ) )
            {
                _left = left;
                _leftTransformer = leftTransformer;
                _right = right;
                _rightTransformer = rightTransformer;
            }

            protected override async Task BackgroundTaskAsync()
            {
                await Task.WhenAll( ChannelLoopAsync( _left, _leftTransformer ), ChannelLoopAsync( _right, _rightTransformer ) );
                Writer.Complete();
            }

            async Task ChannelLoopAsync<T>( ChannelReader<T> reader, Func<T, TOut> transformer )
            {
                await foreach( var item in reader.ReadAllAsync() )
                {
                    Writer.TryWrite( transformer( item ) );
                }
            }
        }


        #endregion
    }

}
