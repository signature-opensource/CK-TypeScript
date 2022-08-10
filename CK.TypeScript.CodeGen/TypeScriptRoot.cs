using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Central TypeScript context with options and a <see cref="Root"/> that contains as many <see cref="TypeScriptFolder"/>
    /// and <see cref="TypeScriptFile"/> as needed that can ultimately be <see cref="TypeScriptFolder.Save"/>d.
    /// </summary>
    public class TypeScriptRoot
    {
        readonly IReadOnlyCollection<(NormalizedPath Path, XElement Config)> _pathsAndConfig;
        readonly bool _pascalCase;
        readonly bool _generateDocumentation;
        readonly bool _generatePocoInterfaces;
        Dictionary<object, object?>? _memory;

        /// <summary>
        /// Initializes a new <see cref="TypeScriptRoot"/>.
        /// </summary>
        /// <param name="pathsAndConfig">Set of output paths with their configuration element. May be empty.</param>
        /// <param name="pascalCase">Whether PascalCase identifiers should be generated instead of camelCase.</param>
        /// <param name="generateDocumentation">Whether documentation should be generated.</param>
        /// <param name="generatePocoInterfaces">Whether IPoco interfaces should be generated.</param>
        public TypeScriptRoot( IReadOnlyCollection<(NormalizedPath Path, XElement Config)> pathsAndConfig,
                               bool pascalCase,
                               bool generateDocumentation,
                               bool generatePocoInterfaces )
        {
            Throw.CheckNotNullArgument( pathsAndConfig );
            _pathsAndConfig = pathsAndConfig;
            _pascalCase = pascalCase;
            _generateDocumentation = generateDocumentation;
            _generatePocoInterfaces = generatePocoInterfaces;
            Root = new TypeScriptFolder( this );
        }

        /// <summary>
        /// Gets whether PascalCase identifiers should be generated instead of camelCase.
        /// This is used by <see cref="ToIdentifier(string)"/>.
        /// </summary>
        public bool PascalCase => _pascalCase;

        /// <summary>
        /// Gets whether documentation should be generated.
        /// </summary>
        public bool GenerateDocumentation => _generateDocumentation;

        /// <summary>
        /// Gets whether IPoco interfaces should be generated.
        /// </summary>
        public bool GeneratePocoInterfaces => _generatePocoInterfaces;

        /// <summary>
        /// Gets or sets the <see cref="IXmlDocumentationCodeRefHandler"/> to use.
        /// When null, <see cref="DocumentationCodeRef.TextOnly"/> is used.
        /// </summary>
        public IXmlDocumentationCodeRefHandler? DocumentationCodeRefHandler { get; set; }

        /// <summary>
        /// Gets the output paths and their configuration element. Never empty.
        /// </summary>
        public IReadOnlyCollection<(NormalizedPath Path, XElement Config)> OutputPaths => _pathsAndConfig;

        /// <summary>
        /// Gets the root folder into which type script files must be generated.
        /// </summary>
        public TypeScriptFolder Root { get; }

        /// <summary>
        /// Gets a shared memory for this root that all <see cref="TypeScriptFolder"/>
        /// and <see cref="TypeScriptFile"/> can use.
        /// </summary>
        /// <remarks>
        /// This is better not to use this directly: hiding this shared storage behind extension methods
        /// like <see cref="TSCodeWriterDocumentationExtensions.AppendDocumentation{T}(T, IActivityMonitor, System.Reflection.MemberInfo)"/> should
        /// be done.
        /// </remarks>
        public IDictionary<object, object?> Memory => _memory ?? (_memory = new Dictionary<object, object?>());

        /// <summary>
        /// Saves this <see cref="Root"/> (all its files and creates the necessary folders)
        /// into <see cref="OutputPaths"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>True on success, false is an error occurred (the error has been logged).</returns>
        public bool Save( IActivityMonitor monitor )
        {
            var barrelPaths = _pathsAndConfig.SelectMany( c => c.Config.Elements( "Barrels" ).Elements( "Barrel" )
                                                     .Select( b => c.Path.Combine( b.Attribute( "Path" )?.Value ) ) );
            var barrels = new HashSet<NormalizedPath>( barrelPaths );
            bool isOk = Root.Save( monitor, _pathsAndConfig.Select( p => p.Path ), barrels.Contains );
            if( !isOk ) return false;
            var dependencies = new Dictionary<string, LibraryImport>();
            // listing dependencies.
            foreach( var file in Root.AllFilesRecursive )
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
                            dependencies[item.Name] = item;
                        }
                    }
                }
            }
            if( !isOk ) return false;
            var sb = new StringBuilder();
            foreach( var item in _pathsAndConfig )
            {
                var path = item.Path;
                var packageJsonPath = Path.Combine( path, "package.json" );
                var depsList = dependencies
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
                sb.Append( @"  ""private"": true
  ""files"": [
    ""dist/""
  ],
  ""main"": ""./dist/cjs/index.js"",
  ""module"": ""./dist/esm/index.js"",
  ""scripts"": {
    ""build"": ""tsc -p tsconfig.json && tsc -p tsconfig-cjs.json""
  }
}" );
                File.WriteAllText( packageJsonPath,
                    sb.ToString()
                );
                sb.Clear();

                File.WriteAllText(
                    Path.Combine( path, "tsconfig.json" ),
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
        ""removeComments"": true,
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
                File.WriteAllText(
                    Path.Combine( path, "tsconfig-cjs.json" ),
@"{
  ""extends"": ""./tsconfig.json"",
  ""compilerOptions"": {
    ""module"": ""CommonJS"",
    ""outDir"": ""./dist/cjs""
  },
}
" );


                var yarnFilePath = "yarn-3.2.2.cjs";
                var yarnBinDir = Path.Combine( path, ".yarn", "releases" );
                Directory.CreateDirectory( yarnBinDir );
                File.Copy( Path.Combine( CurrentAssemblyDirectory, yarnFilePath ), Path.Combine( yarnBinDir, yarnFilePath ), true );



                using( monitor.OpenInfo( $"Running yarn restore..." ) )
                {
                    ProcessRunner( monitor, "node", yarnFilePath, path );
                }

                using( monitor.OpenInfo( $"Running typescript compiler..." ) )
                {
                    ProcessRunner( monitor, "node", $"{yarnFilePath} run build", path );
                }
            }
            return true;
        }

        static void ProcessRunner( IActivityMonitor monitor, string fileName, string arguments, string workingDirectory)
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
            var processLogs = new ChannelReaderMerger<string, string, (string, bool)>(
                new ChannelTextReader( process.StandardOutput ), ( s ) => (s, false),
                new ChannelTextReader( process.StandardOutput ), ( s ) => (s, true)
            );
            process.Start();
            void FlushLogs()
            {
                while( processLogs.TryRead( out var log ) )
                {
                    monitor.Log( log.Item2 ? LogLevel.Error : LogLevel.Trace, log.Item1 );
                }
            }
            while( !processLogs.Completion.IsCompleted )
            {
                FlushLogs();
                process.WaitForExit( 20 ); // avoid closed loop when waiting for log.
            }
            Debug.Assert( process.HasExited );
        }

        static string CurrentAssemblyDirectory = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );

        /// <summary>
        /// Ensures that an identifier follows the <see cref="PascalCase"/> configuration.
        /// Only the first character is handled.
        /// </summary>
        /// <param name="name">The identifier.</param>
        /// <returns>A formatted identifier.</returns>
        public string ToIdentifier( string name ) => ToIdentifier( name, PascalCase );

        /// <summary>
        /// Ensures that an identifier follows the PascalCase xor camelCase convention.
        /// Only the first character is handled.
        /// </summary>
        /// <param name="name">The identifier.</param>
        /// <param name="pascalCase">The target casing.</param>
        /// <returns>A formatted identifier.</returns>
        public static string ToIdentifier( string name, bool pascalCase )
        {
            if( name.Length != 0 && Char.IsUpper( name, 0 ) != pascalCase )
            {
                return pascalCase
                        ? (name.Length == 1
                            ? name.ToUpperInvariant()
                            : Char.ToUpperInvariant( name[0] ) + name.Substring( 1 ))
                        : (name.Length == 1
                            ? name.ToLowerInvariant()
                            : Char.ToLowerInvariant( name[0] ) + name.Substring( 1 ));
            }
            return name;
        }
    }

    public abstract class ChannReaderWrapper<T> : ChannelReader<T>
    {
        readonly Channel<T> _channel;

        protected ChannReaderWrapper( Channel<T> channel )
        {
            Completion = BackgroundTaskAsync();
            _channel = channel;
        }

        protected ChannelWriter<T> Writer => _channel.Writer;

        /// <inheritdoc/>
        public override Task Completion { get; }

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

    public class ChannelTextReader : ChannReaderWrapper<string>
    {
        readonly TextReader _textReader;

        public ChannelTextReader( TextReader textReader ) : base( Channel.CreateUnbounded<string>() )
        {
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

    public class ChannelReaderMerger<TLeft, TRight, TOut> : ChannReaderWrapper<TOut>
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

        protected override Task BackgroundTaskAsync()
            => Task.WhenAll( ChannelLoopAsync( _left, _leftTransformer ), ChannelLoopAsync( _right, _rightTransformer ) );

        async Task ChannelLoopAsync<T>( ChannelReader<T> reader, Func<T, TOut> transformer )
        {
            await foreach( var item in reader.ReadAllAsync() )
            {
                Writer.TryWrite( transformer( item ) );
            }
        }
    }

}
