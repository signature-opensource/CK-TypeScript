using CK.Core;
using CK.Setup;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CK
{
    /// <summary>
    /// Extends <see cref="Testing.IStObjEngineTestHelper"/> for TypeScript support.
    /// </summary>
    public static partial class StObjEngineTestHelperTypeScriptExtensions
    {
        /// <summary>
        /// Gets "<see cref="Testing.IBasicTestHelper.TestProjectFolder"/>/TSGeneratedOnly/<paramref name="testName"/>" path
        /// for tests that only need to generate the "/ck-gen" folder without building it (no TypeScript tooling).
        /// <para>
        /// A .gitignore file with "*" is automatically generated in this folder.
        /// </para>
        /// </summary>
        /// <param name="this">This helper.</param>
        /// <param name="testName">The current test name.</param>
        /// <returns>The TSGeneratedOnly test path.</returns>
        public static NormalizedPath GetTypeScriptGeneratedOnlyTargetProjectPath( this Testing.IMonitorTestHelper @this, [CallerMemberName] string? testName = null )
        {
            var p = @this.TestProjectFolder.AppendPart( "TSGeneratedOnly" );
            if( !Directory.Exists( p ) )
            {
                @this.Monitor.Info( $"Creating folder with .gitignore \"*\": '{p}'." );
                Directory.CreateDirectory( p );
                File.WriteAllText( p.AppendPart( ".gitignore" ), "*" );
            }
            return p.AppendPart( testName );
        }

        /// <summary>
        /// Gets "<see cref="Testing.IBasicTestHelper.TestProjectFolder"/>/TSBuildOnly/<paramref name="testName"/>" path
        /// for tests that must be compiled. Yarn is installed and "/ck-gen" is built. No VSCode support nor TypeScript test
        /// tooling is installed.
        /// </summary>
        /// <param name="this">This helper.</param>
        /// <param name="testName">The current test name.</param>
        /// <returns>The TSBuildOnly test path.</returns>
        public static NormalizedPath GetTypeScriptWithBuildTargetProjectPath( this Testing.IBasicTestHelper @this, [CallerMemberName] string? testName = null )
        {
            return @this.TestProjectFolder.AppendPart( "TSBuildOnly" ).AppendPart( testName );
        }

        /// <summary>
        /// Gets "<see cref="Testing.IBasicTestHelper.TestProjectFolder"/>/TSBuildWithVSCode/<paramref name="testName"/>" path
        /// for tests that must be compiled. Yarn is installed, "/ck-gen" is built and VSCode support is installed.
        /// </summary>
        /// <param name="this">This helper.</param>
        /// <param name="testName">The current test name.</param>
        /// <returns>The TSBuildOnly test path.</returns>
        public static NormalizedPath GetTypeScriptWithBuildAndVSCodeTargetProjectPath( this Testing.IBasicTestHelper @this, [CallerMemberName] string? testName = null )
        {
            return @this.TestProjectFolder.AppendPart( "TSBuildWithVSCode" ).AppendPart( testName );
        }

        /// <summary>
        /// Gets "<see cref="Testing.IBasicTestHelper.TestProjectFolder"/>/TSTests/<paramref name="testName"/>" path
        /// for real tests. Yarn is installed, "/ck-gen" is built, VSCode support is setup and scripts "test" command is
        /// available: GenerateTypeScript methods can be called with it.
        /// </summary>
        /// <param name="this">This helper.</param>
        /// <param name="testName">The current test name.</param>
        /// <returns>The TSTests test path.</returns>
        public static NormalizedPath GetTypeScriptWithTestsSupportTargetProjectPath( this Testing.IBasicTestHelper @this, [CallerMemberName] string? testName = null )
        {
            return @this.TestProjectFolder.AppendPart( "TSTests" ).AppendPart( testName );
        }

        enum GenerateMode
        {
            SkipTypeScriptTooling,
            BuildCKGen,
            BuildCKGenAndVSCodeSupport,
            WithTestSupport
        }

        /// <summary>
        /// Configures (or creates) a <see cref="StObjEngineConfiguration"/> with TypeScript support based on the
        /// <paramref name="targetProjectPath"/>.
        /// </summary>
        /// <param name="helper">This helper.</param>
        /// <param name="configuration">Existing configuration or null to create it. Only a single <see cref="BinPathConfiguration"/> is supported.</param>
        /// <param name="targetProjectPath">Target test path.</param>
        /// <param name="tsTypes">The types to generate in TypeScript.</param>
        /// <returns>The configuration.</returns>
        public static StObjEngineConfiguration ConfigureTypeScript( this Testing.IStObjEngineTestHelper helper,
                                                                    StObjEngineConfiguration? configuration,
                                                                    NormalizedPath targetProjectPath,
                                                                    params Type[] tsTypes )
        {
            Throw.CheckArgument( targetProjectPath.Parts.Count > 2 );
            var testMode = targetProjectPath.Parts[^2] switch
            {
                "TSGeneratedOnly" => GenerateMode.SkipTypeScriptTooling,
                "TSBuildOnly" => GenerateMode.BuildCKGen,
                "TSBuildWithVSCode" => GenerateMode.BuildCKGenAndVSCodeSupport,
                "TSTests" => GenerateMode.WithTestSupport,
                _ => Throw.ArgumentException<GenerateMode>( $"Unsupported target project path: '{targetProjectPath}'.{Environment.NewLine}" +
                                                            $"The target path must be obtained with TestHelper methods GetTypeScriptGeneratedOnlyTargetProjectPath()," +
                                                            $"GetTypeScriptWithBuildTargetProjectPath(), GetTypeScriptWithBuildAndVSCodeTargetProjectPath() or " +
                                                            $"GetTypeScriptWithTestsSupportTargetProjectPath()." )
            };

            configuration ??= new StObjEngineConfiguration();
            var tsConfig = new TypeScriptAspectConfiguration();
            configuration.Aspects.Add( tsConfig );

            BinPathConfiguration b;
            if( configuration.BinPaths.Count == 0 )
            {
                b = new BinPathConfiguration();
                configuration.BinPaths.Add( b );
            }
            else if( configuration.BinPaths.Count == 1 )
            {
                b = configuration.BinPaths[0];
            }
            else
            {
                return Throw.ArgumentException<StObjEngineConfiguration>( "StObjEngineConfiguration multiple BinPaths is not supported." );
            }

            if( b.ProjectPath.IsEmptyPath )
            {
                b.ProjectPath = helper.TestProjectFolder;
            }
            var tsBinPathConfig = new TypeScriptAspectBinPathConfiguration
            {
                TargetProjectPath = targetProjectPath,
                SkipTypeScriptTooling = testMode == GenerateMode.SkipTypeScriptTooling,
                EnsureTestSupport = testMode == GenerateMode.WithTestSupport,
                AutoInstallVSCodeSupport = testMode >= GenerateMode.BuildCKGenAndVSCodeSupport,
                AutoInstallYarn = testMode >= GenerateMode.BuildCKGen,
                GitIgnoreCKGenFolder = true
            };
            tsBinPathConfig.Types.AddRange( tsTypes.Select( t => new TypeScriptTypeConfiguration( t ) ) );
            b.AspectConfigurations.Add( tsBinPathConfig.ToXml() );
            return configuration;
        }

        sealed class MonoCollectorResolver : IStObjCollectorResultResolver
        {
            readonly Testing.IStObjEngineTestHelper _helper;
            readonly Type[] _types;

            public MonoCollectorResolver( Testing.IStObjEngineTestHelper helper, params Type[] types )
            {
                _helper = helper;
                _types = types;
            }

            public StObjCollectorResult? GetResult( RunningBinPathGroup g )
            {
                return _helper.GetSuccessfulResult( _helper.CreateStObjCollector( _types ) );
            }
        }

        /// <summary>
        /// Helper that runs a <see cref="StObjEngine"/> with a <see cref="ConfigureTypeScript(Testing.IStObjEngineTestHelper, StObjEngineConfiguration?, NormalizedPath, Type[])"/>
        /// configuration. TypeScript "/ck-gen" folder with the provided <paramref name="types"/> is generated (whether TypeScript tooling
        /// is setup or run depends on the <paramref name="targetProjectPath"/>.
        /// </summary>
        /// <param name="helper">This helper.</param>
        /// <param name="targetProjectPath">The target TypeScript project path.</param>
        /// <param name="types">The types to generate in TypeScript.</param>
        public static void GenerateTypeScript( this Testing.IStObjEngineTestHelper helper,
                                               NormalizedPath targetProjectPath,
                                               params Type[] types )
        {
            GenerateTypeScript( helper, targetProjectPath, types, types );
        }

        /// <summary>
        /// Helper that runs a <see cref="StObjEngine"/> with a <see cref="ConfigureTypeScript(Testing.IStObjEngineTestHelper, StObjEngineConfiguration?, NormalizedPath, Type[])"/>
        /// configuration. TypeScript "/ck-gen" folder with the provided <paramref name="types"/> is generated (whether TypeScript tooling
        /// is setup or run depends on the <paramref name="targetProjectPath"/>.
        /// </summary>
        /// <param name="helper">This helper.</param>
        /// <param name="targetProjectPath">The target TypeScript project path.</param>
        /// <param name="configureEngine">Optional engine configurator.</param>
        /// <param name="types">The types to generate in TypeScript.</param>
        public static void GenerateTypeScript( this Testing.IStObjEngineTestHelper helper,
                                               NormalizedPath targetProjectPath,
                                               Action<StObjEngineConfiguration> configureEngine,
                                               params Type[] types )
        {
            GenerateTypeScript( helper, targetProjectPath, types, types, configureEngine );
        }

        /// <summary>
        /// Helper that runs a <see cref="StObjEngine"/> with a <see cref="ConfigureTypeScript(Testing.IStObjEngineTestHelper, StObjEngineConfiguration?, NormalizedPath, Type[])"/>
        /// configuration. TypeScript "/ck-gen" folder with the provided <paramref name="tsTypes"/> is generated (whether TypeScript tooling
        /// is setup or run depends on the <paramref name="targetProjectPath"/>.
        /// </summary>
        /// <param name="helper">This helper.</param>
        /// <param name="targetProjectPath">The target TypeScript project path.</param>
        /// <param name="registeredTypes">The types to register in the <see cref="StObjCollector"/>.</param>
        /// <param name="tsTypes">The types to generate in TypeScript.</param>
        /// <param name="configureEngine">Optional engine configurator.</param>
        /// <param name="cSharpCompile">Optionally parse or compile the generated C# code of the single <see cref="BinPathConfiguration"/>.</param>
        public static void GenerateTypeScript( this Testing.IStObjEngineTestHelper helper,
                                               NormalizedPath targetProjectPath,
                                               IEnumerable<Type> registeredTypes,
                                               IEnumerable<Type> tsTypes,
                                               Action<StObjEngineConfiguration>? configureEngine = null,
                                               CompileOption cSharpCompile = CompileOption.None )
        {
            StObjEngineConfiguration config = ConfigureTypeScript( helper, null, targetProjectPath, tsTypes.ToArray() );
            config.BinPaths[0].CompileOption = cSharpCompile;
            configureEngine?.Invoke( config );
            var engine = new StObjEngine( helper.Monitor, config );
            var collectorResults = new MonoCollectorResolver( helper, registeredTypes.ToArray() );
            engine.Run( collectorResults ).Success.Should().BeTrue( "StObjEngine.Run worked." );
        }

        /// <summary>
        /// Disposable running test created by <see cref="CreateTypeScriptRunner(Testing.IStObjEngineTestHelper, NormalizedPath, Dictionary{string, string}?, string)"/>.
        /// </summary>
        public class TypeScriptRunner : IAsyncDisposable
        {
            readonly Testing.IStObjEngineTestHelper _helper;
            readonly NormalizedPath _targetProjectPath;
            readonly Dictionary<string, string>? _environmentVariables;
            readonly string _yarnCommand;
            readonly Action? _jestDispose;
            bool _isDisposed;
            List<object>? _onDisposeList;

            internal TypeScriptRunner( Testing.IStObjEngineTestHelper helper,
                                       NormalizedPath targetProjectPath,
                                       Dictionary<string, string>? environmentVariables,
                                       string yarnCommand,
                                       Action? jestDispose )
            {
                _helper = helper;
                _targetProjectPath = targetProjectPath;
                _jestDispose = jestDispose;
                _environmentVariables = environmentVariables;
                _yarnCommand = yarnCommand;
            }

            /// <summary>
            /// Runs the yarn command and fails on error.
            /// </summary>
            public void Run()
            {
                YarnHelper.RunYarn( _helper.Monitor, _targetProjectPath, _yarnCommand, _environmentVariables )
                    .Should().BeTrue( $"'yarn {_yarnCommand}' should be sucessfull." );
            }

            /// <summary>
            /// Adds cleanup function that will be called by <see cref="DisposeAsync"/>.
            /// </summary>
            /// <param name="onDispose">An asynchronous cleanup function.</param>
            public void OnDispose( Func<Task> onDispose )
            {
                Throw.CheckNotNullArgument( onDispose );
                _onDisposeList ??= new List<object>();
                _onDisposeList.Add( onDispose );
            }

            /// <summary>
            /// Adds cleanup function that will be called by <see cref="DisposeAsync"/>.
            /// </summary>
            /// <param name="onDispose">An asynchronous cleanup function.</param>
            public void OnDispose( Func<ValueTask> onDispose )
            {
                Throw.CheckNotNullArgument( onDispose );
                _onDisposeList ??= new List<object>();
                _onDisposeList.Add( onDispose );
            }

            /// <summary>
            /// Adds cleanup function that will be called by <see cref="DisposeAsync"/>.
            /// </summary>
            /// <param name="onDispose">A synchronous cleanup function.</param>
            public void OnDispose( Action onDispose )
            {
                Throw.CheckNotNullArgument( onDispose );
                _onDisposeList ??= new List<object>();
                _onDisposeList.Add( onDispose );
            }

            /// <summary>
            /// Must be called (using should be used) to revert any test setup side-effects that
            /// may have been done on the environment.
            /// </summary>
            public async ValueTask DisposeAsync()
            {
                if( !_isDisposed )
                {
                    _isDisposed = true;
                    _jestDispose?.Invoke();
                    if( _onDisposeList != null )
                    {
                        foreach( var o in _onDisposeList )
                        {
                            if( o is Func<Task> aT ) await aT();
                            if( o is Func<ValueTask> aV ) await aV();
                            else ((Action)o).Invoke();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="TypeScriptRunner"/> that MUST be disposed once <see cref="TypeScriptRunner.Run()"/> has been called:
        /// <code>
        /// await using var runner = TestHelper.CreateTypeScriptTestRunner( targetProjectPath );
        /// // await CK.Testing.SuspendAsync();
        /// runner.Run();
        /// </code>
        /// <para>
        /// This 2-steps pattern enables to temporarily inserts a <see cref="CK.Testing.Monitoring.IMonitorTestHelperCore.SuspendAsync"/>
        /// before calling the <see cref="TypeScriptRunner.Run()"/> in order to be able to keep a running context alive while
        /// working on the TypeScript side (fixing, debugging, analyzing, etc.) TypeScript tests. 
        /// </para>
        /// </summary>
        /// <param name="this">This helper.</param>
        /// <param name="targetProjectPath">The target test path.</param>
        /// <param name="environmentVariables">Optional environment variables to set.</param>
        /// <param name="command">Yarn command that will be executed.</param>
        public static TypeScriptRunner CreateTypeScriptRunner( this Testing.IStObjEngineTestHelper @this,
                                                               NormalizedPath targetProjectPath,
                                                               Dictionary<string, string>? environmentVariables = null,
                                                               string command = "test" )
        {
            YarnHelper.PrepareJestRun( @this.Monitor, targetProjectPath, environmentVariables, out var afterRun ).Should().BeTrue();
            return new TypeScriptRunner( @this, targetProjectPath, environmentVariables, command, afterRun );
        }

    }
}
