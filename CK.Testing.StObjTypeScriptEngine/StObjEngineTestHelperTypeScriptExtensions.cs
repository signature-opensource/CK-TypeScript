using CK.Core;
using CK.Setup;
using CK.Testing;
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
        /// for real tests. Yarn is installed, "/ck-gen" is built, VSCode support is setup, a script "test" command is
        /// available and a "src/sample.spec.ts" file is ready to be used.
        /// <para>
        /// <see cref="CreateTypeScriptRunner(Testing.IStObjEngineTestHelper, NormalizedPath, Dictionary{string, string}?, string)"/> can be used to execute
        /// the TypeScript tests.
        /// </para>
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
        /// Ensures that a <see cref="TypeScriptAspect"/> is available in <see cref="EngineConfiguration.Aspects"/> and that the <see cref="EngineConfiguration.FirstBinPath"/>
        /// contains a <see cref="TypeScriptBinPathAspectConfiguration"/> configured for the <paramref name="targetProjectPath"/>.
        /// </summary>
        /// <param name="helper">This helper.</param>
        /// <param name="engineConfiguration">The engine configuration to configure.</param>
        /// <param name="targetProjectPath">
        /// Obtained from <see cref="GetTypeScriptWithTestsSupportTargetProjectPath(IBasicTestHelper, string?)"/> or one of
        /// the other similar methods.
        /// </param>
        /// <param name="tsTypes">The types to generate in TypeScript.</param>
        public static EngineConfiguration EnsureTypeScriptConfigurationAspect( this IMonitorTestHelper helper,
                                                                               EngineConfiguration engineConfiguration,
                                                                               NormalizedPath targetProjectPath,
                                                                               params Type[] tsTypes )
        {
            Throw.CheckNotNullArgument( engineConfiguration );
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
            var typeScriptAspect = engineConfiguration.Aspects.OfType<TypeScriptAspectConfiguration>().SingleOrDefault();
            if( typeScriptAspect == null )
            {
                typeScriptAspect = new TypeScriptAspectConfiguration();
                engineConfiguration.AddAspect( typeScriptAspect );
            }
            var tsBinPathAspect = engineConfiguration.FirstBinPath.FindAspect<TypeScriptBinPathAspectConfiguration>();
            if( tsBinPathAspect == null )
            {
                tsBinPathAspect = new TypeScriptBinPathAspectConfiguration();
                engineConfiguration.FirstBinPath.AddAspect( tsBinPathAspect );
            }

            tsBinPathAspect.TargetProjectPath = targetProjectPath;
            tsBinPathAspect.SkipTypeScriptTooling = testMode == GenerateMode.SkipTypeScriptTooling;
            tsBinPathAspect.EnsureTestSupport = testMode == GenerateMode.WithTestSupport;
            tsBinPathAspect.AutoInstallVSCodeSupport = testMode >= GenerateMode.BuildCKGenAndVSCodeSupport;
            tsBinPathAspect.AutoInstallYarn = testMode >= GenerateMode.BuildCKGen;
            tsBinPathAspect.GitIgnoreCKGenFolder = true;
            tsBinPathAspect.Types.Clear();
            tsBinPathAspect.Types.AddRange( tsTypes.Select( t => new TypeScriptTypeConfiguration( t ) ) );
            return engineConfiguration;
        }

        public static StObjEngineResult RunEngineWithTypeScript( this IMonitorTestHelper helper,
                                                                 EngineConfiguration engineConfiguration,
                                                                 NormalizedPath targetProjectPath,
                                                                 ISet<Type> types,
                                                                 params Type[] tsTypes )
        {
            EnsureTypeScriptConfigurationAspect( helper, engineConfiguration, targetProjectPath, tsTypes );
            return helper.RunEngine( engineConfiguration, types );
        }

        public static StObjEngineResult GenerateTypeScript( this IMonitorTestHelper helper,
                                                            NormalizedPath targetProjectPath,
                                                            params Type[] types )
        {
            return GenerateTypeScript( helper, targetProjectPath, helper.CreateTypeCollector( types ), types );
        }

        public static StObjEngineResult GenerateTypeScript( this IMonitorTestHelper helper,
                                                            NormalizedPath targetProjectPath,
                                                            ISet<Type> registeredTypes,
                                                            params Type[] tsTypes )
        {
            var engineConfiguration = helper.CreateDefaultEngineConfiguration( generateSourceFiles: false, CompileOption.None );
            EnsureTypeScriptConfigurationAspect( helper, engineConfiguration, targetProjectPath, tsTypes );
            var r = helper.RunEngine( engineConfiguration, registeredTypes );
            r.Success.Should().BeTrue( "Engine.Run worked." );
            return r;
        }

        /// <summary>
        /// Creates a <see cref="TypeScriptRunner"/> that MUST be disposed once <see cref="TypeScriptRunner.Run()"/> has been called:
        /// <code>
        /// await using var runner = TestHelper.CreateTypeScriptTestRunner( targetProjectPath );
        /// //
        /// // The TypeScript environment is setup with Jest and can be used to develop/fix the TS code.
        /// // By using the SuspendAsync trick, when running in Debug, the C# application is running (a web server
        /// // for instance is available and can be called by the TypeScript tests).
        /// //
        /// // await TestHelper.SuspendAsync( resume => resume );
        /// //
        /// runner.Run(); // This executes the TypeScript tests.
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
        public static TypeScriptRunner CreateTypeScriptRunner( this IMonitorTestHelper @this,
                                                               NormalizedPath targetProjectPath,
                                                               Dictionary<string, string>? environmentVariables = null,
                                                               string command = "test" )
        {
            YarnHelper.PrepareJestRun( @this.Monitor, targetProjectPath, environmentVariables, out var afterRun ).Should().BeTrue();
            return new TypeScriptRunner( @this, targetProjectPath, environmentVariables, command, afterRun );
        }

    }
}
