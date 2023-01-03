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
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CK.Setup
{
    static class YarnPackageGenerator
    {
        static readonly string _yarnFileName = "yarn-3.2.2.cjs";

        internal static bool SaveBuildConfig( IActivityMonitor monitor, TypeScriptGenerator root )
        {
            using var gLog = monitor.OpenInfo( $"Saving TypeScript and Yarn build configuration files..." );

            var dependencies = new Dictionary<string, LibraryImport>
            {
                { "typescript", new LibraryImport( "typescript", "4.7.4", DependencyKind.DevDependency ) }
            };

            // listing dependencies.
            bool isOk = true;
            foreach( var file in root.Root.AllFilesRecursive )
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
                            isOk = false;
                        }
                        if( item.DependencyKind > prevImport.DependencyKind )
                        {
                            monitor.Info( $"Dependency {item.Name} had dependency kind {prevImport.DependencyKind}, {file.Name} is upgrading it to {item.DependencyKind}" );
                            dependencies[item.Name] = item;
                        }
                    }
                }
            }
            if( !isOk ) return false;

            var sb = new StringBuilder();
            foreach( var item in root.OutputPaths )
            {
                var path = item.Path;
                var packageJsonPath = Path.Combine( path, "package.json" );
                var depsList = dependencies
                    .Concat(
                        dependencies
                            .Where( s => s.Value.DependencyKind == DependencyKind.PeerDependency )
                            .Select(
                            s => new KeyValuePair<string, LibraryImport>(
                                s.Key, new LibraryImport( s.Value.Name, s.Value.Version, DependencyKind.DevDependency )
                            ) )
                    )
                    .GroupBy( s => s.Value.DependencyKind, s => $@"    ""{s.Value.Name}"":""{s.Value.Version}""" )
                    .Select( s => (s.Key switch
                    {
                        DependencyKind.Dependency => "dependencies",
                        DependencyKind.DevDependency => "devDependencies",
                        DependencyKind.PeerDependency => "peerDependencies",
                        _ => throw new InvalidOperationException()
                    }, string.Join( ",\n", s )) );
                sb.Append( @"{
  ""name"": ""@signature/generated"",
" );
                foreach( var deps in depsList )
                {
                    sb.AppendLine( $@"  ""{deps.Item1}"":{{" );
                    sb.Append( deps.Item2 );
                    sb.AppendLine( "\n  }," );
                }
                sb.Append(
@"  ""private"": true,
  ""files"": [
    ""dist/""
  ],
  ""main"": ""./dist/cjs/index.js"",
  ""module"": ""./dist/esm/index.js"",
  ""scripts"": {
    ""build"": ""tsc -p tsconfig.json && tsc -p tsconfig-cjs.json""
  }
}" );
                monitor.Trace( $"Creating '{packageJsonPath}'." );
                File.WriteAllText( packageJsonPath, sb.ToString() );
                sb.Clear();

                var tsConfigFile = Path.Combine( path, "tsconfig.json" );
                monitor.Trace( $"Creating '{tsConfigFile}'." );
                File.WriteAllText( tsConfigFile,
     @"{
    ""compilerOptions"": {
        ""strict"": true,
        ""target"": ""es5"",
        ""module"": ""ES6"",
        ""moduleResolution"": ""node"",
        ""lib"": [""es2015"", ""es2016"", ""es2017"", ""dom""],
        ""baseUrl"": ""./src"",
        ""outDir"": ""./dist/esm"",
        ""sourceMap"": true,
        ""declaration"": true,
        ""esModuleInterop"": true,
        ""resolveJsonModule"": true,
        ""rootDir"": ""ts/src""
    },
    ""include"": [
        ""ts/src/**/*""
    ],
    ""exclude"": [
        "".vscode"",
        ""node_modules"",
        ""spec""
    ]
}
" );
                var tsConfigCJSFile = Path.Combine( path, "tsconfig-cjs.json" );
                monitor.Trace( $"Creating '{tsConfigCJSFile}'." );
                File.WriteAllText( tsConfigCJSFile,
@"{
  ""extends"": ""./tsconfig.json"",
  ""compilerOptions"": {
    ""module"": ""CommonJS"",
    ""outDir"": ""./dist/cjs""
  },
}
" );
                CreateYarnInstall( monitor, path );
            }
            return true;
        }

        private static void CreateYarnInstall( IActivityMonitor monitor, NormalizedPath path )
        {
            var yarnPath = YarnHelper.TryFindYarn( path.RemoveLastPart() );
            if( yarnPath.HasValue )
            {
                monitor.Info( $"Yarn install found at {yarnPath}, skipping adding yarn." );
                return;
            }
            monitor.Info( "No yarn install found, we will add our own." );
            var yarnrcFile = Path.Combine( path, ".yarnrc.yml" );
            monitor.Trace( $"Creating '{yarnrcFile}'." );
            File.WriteAllText( yarnrcFile,
@"yarnPath: .yarn/releases/yarn-3.2.2.cjs
enableImmutableInstalls: false" );

            var yarnBinDir = Path.Combine( path, ".yarn", "releases" );
            File.WriteAllText( Path.Combine( path, ".gitignore" ), "*" );
            monitor.Trace( $"Extracting '{_yarnFileName}' to '{yarnBinDir}'." );
            Directory.CreateDirectory( yarnBinDir );
            var currAssembly = Assembly.GetExecutingAssembly();
            using( var yarnBinStream = currAssembly.GetManifestResourceStream( currAssembly.GetName().Name + "." + _yarnFileName )! )
            using( var fileStream = File.OpenWrite( Path.Combine( yarnBinDir, _yarnFileName ) ) )
            {
                yarnBinStream.CopyTo( fileStream );
            }
        }

        internal static bool RunNodeBuild( IActivityMonitor monitor, TypeScriptGenerator root )
        {
            using var gLog = monitor.OpenInfo( $"Building TypeScript projects..." );
            
            foreach( var item in root.OutputPaths )
            {
                var yarnBinJS = YarnHelper.TryFindYarn( item.Path );
                if( !yarnBinJS.HasValue )
                {
                    monitor.Error( "Could not find yarn binaries." );
                    return false;
                }
                using( monitor.OpenInfo( $"Running yarn restore in {item.Path}." ) )
                {
                    int code = ProcessRunner( monitor, "node", yarnBinJS, item.Path );
                    if( code != 0 )
                    {
                        monitor.Error( "Exit code is not 0." );
                        return false;
                    }
                }

                using( monitor.OpenInfo( $"Running typescript compiler on {item.Path}" ) )
                {
                    int code = ProcessRunner( monitor, "node", $"{yarnBinJS} run build", item.Path );
                    if( code != 0 )
                    {
                        monitor.Error( "Exit code is not 0." );
                        return false;
                    }
                }
            }
            return true;
        }

        #region ProcessRunner for NodeBuild

        static int ProcessRunner( IActivityMonitor monitor, string fileName, string arguments, string workingDirectory )
        {
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
            left.Start();
            right.Start();
            var processLogs = new ChannelReaderMerger<string, string, (string, bool)>(
                left, ( s ) => (s, false),
                right, ( s ) => (s, true)
            );
            processLogs.Start();
            void FlushLogs()
            {
                while( processLogs.TryRead( out var log ) )
                {
                    monitor.Log( log.Item2 ? LogLevel.Error : LogLevel.Trace, log.Item1 );
                }
            }

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
        }

        abstract class ChannelReaderWrapper<T> : ChannelReader<T>
        {
            readonly Channel<T> _channel;
            Task _completion;

            protected ChannelReaderWrapper( Channel<T> channel )
            {
                _channel = channel;
            }

            public void Start()
            {
                _completion = BackgroundTaskAsync();
            }

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

            public ChannelReaderMerger( ChannelReader<TLeft> left, Func<TLeft, TOut> leftTransformer, ChannelReader<TRight> right, Func<TRight, TOut> rightTransformer, bool singleReader = false )
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
