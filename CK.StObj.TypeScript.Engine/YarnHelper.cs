using CK.Core;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CK.Setup
{
    static class YarnHelper
    {
        const string _yarnFileName = $"yarn-{TypeScriptAspectBinPathConfiguration.AutomaticYarnVersion}.cjs";
        const string _autoYarnPath = $".yarn/releases/{_yarnFileName}";

        /// <summary>
        /// Locates yarn in <paramref name="workingDirectory"/> or above and calls it with the provided <paramref name="command"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="command">The command to run.</param>
        /// <returns>True on success, false if yarn cannot be found or the process failed.</returns>
        public static bool RunYarn( IActivityMonitor monitor, NormalizedPath workingDirectory, string command )
        {
            var yarnPath = TryFindYarn( workingDirectory );
            if( yarnPath.HasValue )
            {
                return DoRunYarn( monitor, workingDirectory, command, yarnPath.Value );
            }
            monitor.Error( $"Unable to find yarn in '{workingDirectory}' or above." );
            return false;
        }

        /// <summary>
        /// Generates "package.json", "tsconfig.json" and "tsconfig-cjs.json".
        /// </summary>
        internal static bool SaveBuildConfig( IActivityMonitor monitor, NormalizedPath outputPath, TypeScriptContext g )
        {
            using var gLog = monitor.OpenInfo( $"Saving TypeScript and Yarn build configuration files..." );

            var reusable = new StringBuilder();
            return GeneratePackageJson( monitor, outputPath, g, reusable )
                   && GenerateTSConfigJson( monitor, outputPath, g, reusable )
                   && GenerateTSConfigCJSJson( monitor, outputPath, g, reusable );

            static bool GeneratePackageJson( IActivityMonitor monitor, NormalizedPath outputPath, TypeScriptContext g, StringBuilder sb )
            {
                sb.Clear();
                var packageJsonPath = Path.Combine( outputPath, "package.json" );
                using( monitor.OpenTrace( $"Creating '{packageJsonPath}'." ) )
                {
                    bool success = true;
                    var dependencies = new Dictionary<string, LibraryImport>
                                        {
                                            { "typescript", new LibraryImport( "typescript", "4.7.4", DependencyKind.DevDependency ) }
                                        };
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
                        },
                                       string.Join( ",\n", s )) );
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
                                                            "rootDir": "ts/src"
                                                        },
                                                        "include": [
                                                            "ts/src/**/*"
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
            var yarnPath = TryFindYarn( targetProjectPath );
            if( yarnPath.HasValue )
            {
                monitor.Info( $"Yarn found at '{yarnPath}'." );
                return yarnPath.Value;
            }
            if( autoInstall )
            {
                monitor.Info( $"No yarn found, we will add our own {_autoYarnPath} in '{targetProjectPath}'." );
                var yarnrcFile = Path.Combine( targetProjectPath, ".yarnrc.yml" );
                monitor.Trace( $"Creating '{yarnrcFile}'." );
                File.WriteAllText( yarnrcFile, $"""
                                            yarnPath: {_autoYarnPath}
                                            enableImmutableInstalls: false
                                            """ );

                var yarnBinDir = Path.Combine( targetProjectPath, ".yarn", "releases" );
                File.WriteAllText( Path.Combine( targetProjectPath, ".gitignore" ), "*" );
                monitor.Trace( $"Extracting '{_yarnFileName}' to '{yarnBinDir}'." );
                Directory.CreateDirectory( yarnBinDir );
                var a = Assembly.GetExecutingAssembly();
                Throw.DebugAssert( a.GetName().Name == "CK.StObj.TypeScript.Engine" );
                using( var yarnBinStream = a.GetManifestResourceStream( $"CK.StObj.TypeScript.Engine.{_yarnFileName}" ) )
                using( var fileStream = File.OpenWrite( Path.Combine( yarnBinDir, _yarnFileName ) ) )
                {
                    yarnBinStream!.CopyTo( fileStream );
                }
                return targetProjectPath.Combine( _autoYarnPath );
            }
            monitor.Warn( $"No yarn found in '{targetProjectPath}' or above and AutoInstallYarn is false." );
            return null;
        }

        internal static void InstallVSCodeSupport( IActivityMonitor monitor, NormalizedPath targetProjectPath, NormalizedPath yarnPath )
        {
            bool isHere = Directory.Exists( targetProjectPath.AppendPart( ".vscode" ) )
                          && Directory.Exists( targetProjectPath.Combine( ".yarn/sdks" ) );
            if( !isHere )
            {
                DoRunYarn( monitor, targetProjectPath, "dlx @yarnpkg/sdks vscode", yarnPath );
            }
        }

        static NormalizedPath? TryFindYarn( NormalizedPath currentDirectory )
        {
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
            }
            return default;
        }

        internal static bool EnsurePackageJsonWithCKGenWorkspace( IActivityMonitor monitor, NormalizedPath targetProjectPath )
        {
            var projectJsonPath = targetProjectPath.AppendPart( "package.json" );
            if( !File.Exists( projectJsonPath ) )
            {
                monitor.Info( $"Creating a minimal '{projectJsonPath}'." );
                WriteMinimalPackageJson( targetProjectPath, projectJsonPath );
                return true;
            }
            JsonNode? doc;
            try
            {
                using var f = File.OpenRead( projectJsonPath );
                doc = JsonNode.Parse( f,
                                        nodeOptions: null,
                                        new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Allow } );
            }
            catch ( Exception ex )
            {
                monitor.Warn( $"Unable to read file '{projectJsonPath}'. Skipping ck-gen workspace configuration.", ex );
                return false;
            }
            if( doc is not JsonObject o )
            {
                monitor.Warn( $"File '{projectJsonPath}' doesn't contain a Json object. Skipping ck-gen workspace configuration." );
                return false;
            }
            var workspaces = o["workspaces"];
            if( workspaces == null ) o.Add("workspaces", new JsonArray( "ck-gen" ) );
            else 
            {
                if( workspaces is not JsonArray a )
                {
                    monitor.Error( $"Error in '{projectJsonPath}': workspaces property is not an array. Skipping ck-gen workspace configuration." );
                    return false;
                }
                if( a.Any( x => x is JsonValue v && v.TryGetValue<string>( out var s ) && s == "ck-gen" ) )
                {
                    return true;
                }
                a.Add( "ck-gen" );
            }
            using( monitor.OpenInfo( $"Rewriting '{projectJsonPath}' with ck-gen workspace." ) )
            {
                try
                {
                    using var fOut = File.OpenRead( projectJsonPath );
                    using var wOut = new Utf8JsonWriter( fOut, new JsonWriterOptions { Indented = true } );
                    doc.WriteTo( wOut );
                }
                catch( Exception ex )
                {
                    monitor.Error( $"While writing '{projectJsonPath}'.", ex );
                    return false;
                }
            }
            return true;

            static void WriteMinimalPackageJson( NormalizedPath targetProjectPath, NormalizedPath projectJsonPath )
            {
                File.WriteAllText( projectJsonPath,
                   $$"""
                     {
                       "name": "{{targetProjectPath.LastPart.ToLowerInvariant()}}",
                       "private": true,
                       "workspaces":["ck-gen"]
                     }
                    """ );
            }
        }

        internal static bool DoRunYarn( IActivityMonitor monitor, NormalizedPath workingDirectory, string command, NormalizedPath yarnPath )
        {
            using( monitor.OpenInfo( $"Running 'yarn {command}' in '{workingDirectory}'." ) )
            {
                int code = RunProcess( monitor, "node", $"{yarnPath} {command}", workingDirectory );
                if( code != 0 )
                {
                    monitor.CloseGroup( $"Exit code {code}." );
                    return false;
                }
            }
            return true;
        }

        #region ProcessRunner for NodeBuild

        static int RunProcess( IActivityMonitor monitor, string fileName, string arguments, string workingDirectory )
        {
            monitor.Trace( $"RunProcess: '{fileName} {arguments}'." );
            var process = new Process
            {
                StartInfo = new ProcessStartInfo( fileName, arguments )
                {
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
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
