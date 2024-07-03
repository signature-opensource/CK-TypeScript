using CK.Core;
using CK.Setup;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CK.Testing
{
    /// <summary>
    /// Extends <see cref="IMonitorTestHelper"/> for TypeScript support.
    /// </summary>
    public static partial class TSTestHelperExtensions
    {
        /// <summary>
        /// Gets "<see cref="IBasicTestHelper.TestProjectFolder"/>/TSGeneratedOnly/<paramref name="testName"/>" path
        /// for tests that only need to generate the "/ck-gen" folder without building it (no TypeScript tooling).
        /// <para>
        /// A .gitignore file with "*" is automatically generated in this folder.
        /// </para>
        /// </summary>
        /// <param name="this">This helper.</param>
        /// <param name="testName">The current test name.</param>
        /// <returns>The TSGeneratedOnly test path.</returns>
        public static NormalizedPath GetTypeScriptGeneratedOnlyTargetProjectPath( this IMonitorTestHelper @this, [CallerMemberName] string? testName = null )
        {
            var p = @this.TestProjectFolder.AppendPart( "TSGeneratedOnly" );
            if( !Directory.Exists( p ) )
            {
                @this.Monitor.Info( $"Creating folder with .gitignore \"*\": '{p}'." );
                Directory.CreateDirectory( p );
                File.WriteAllText( p.AppendPart( ".gitignore" ), "*" );
            }
            return p.AppendPart( RemoveAsyncSuffix( testName ) );
        }

        static string RemoveAsyncSuffix( string? testName )
        {
            Throw.DebugAssert( testName != null );
            if( testName.EndsWith( "_Async" ) ) testName = testName.Substring( 0, testName.Length - 6 );
            return testName;
        }

        /// <summary>
        /// Gets "<see cref="IBasicTestHelper.TestProjectFolder"/>/TSBuildOnly/<paramref name="testName"/>" path
        /// for tests that must be compiled. Yarn is installed and "/ck-gen" is built. No VSCode support nor TypeScript test
        /// tooling is installed.
        /// </summary>
        /// <param name="this">This helper.</param>
        /// <param name="testName">The current test name.</param>
        /// <returns>The TSBuildOnly test path.</returns>
        public static NormalizedPath GetTypeScriptWithBuildTargetProjectPath( this IBasicTestHelper @this, [CallerMemberName] string? testName = null )
        {
            return @this.TestProjectFolder.AppendPart( "TSBuildOnly" ).AppendPart( RemoveAsyncSuffix( testName ) );
        }

        /// <summary>
        /// Gets "<see cref="IBasicTestHelper.TestProjectFolder"/>/TSBuildWithVSCode/<paramref name="testName"/>" path
        /// for tests that must be compiled. Yarn is installed, "/ck-gen" is built and VSCode support is installed.
        /// </summary>
        /// <param name="this">This helper.</param>
        /// <param name="testName">The current test name.</param>
        /// <returns>The TSBuildWithVSCode test path.</returns>
        public static NormalizedPath GetTypeScriptWithBuildAndVSCodeTargetProjectPath( this IBasicTestHelper @this, [CallerMemberName] string? testName = null )
        {
            return @this.TestProjectFolder.AppendPart( "TSBuildWithVSCode" ).AppendPart( RemoveAsyncSuffix( testName ) );
        }

        /// <summary>
        /// Gets "<see cref="IBasicTestHelper.TestProjectFolder"/>/TSTests/<paramref name="testName"/>" path
        /// for real tests. Yarn is installed, "/ck-gen" is built, VSCode support is setup, a script "test" command is
        /// available and a "src/sample.spec.ts" file is ready to be used.
        /// <para>
        /// <see cref="CreateTypeScriptRunner(IMonitorTestHelper, NormalizedPath, Dictionary{string, string}?, string)"/> can be used to execute
        /// the TypeScript tests.
        /// </para>
        /// </summary>
        /// <param name="this">This helper.</param>
        /// <param name="testName">The current test name.</param>
        /// <returns>The TSTests test path.</returns>
        public static NormalizedPath GetTypeScriptWithTestsSupportTargetProjectPath( this IBasicTestHelper @this, [CallerMemberName] string? testName = null )
        {
            return @this.TestProjectFolder.AppendPart( "TSTests" ).AppendPart( RemoveAsyncSuffix( testName ) );
        }

        /// <summary>
        /// Gets "<see cref="IBasicTestHelper.TestProjectFolder"/>/TSBuildAndTests/<paramref name="testName"/>" path
        /// for real tests. Yarn is installed, "/ck-gen" is built, VSCode support is setup, a script "test" command is
        /// available and a "src/sample.spec.ts" file is ready to be used. Any modification in the /ck-gen folder
        /// is preserved and the setup will fail until the modified files are deleted or the generated code exactly matches
        /// the modified files.
        /// <para>
        /// <see cref="CreateTypeScriptRunner(IMonitorTestHelper, NormalizedPath, Dictionary{string, string}?, string)"/> can be used to execute
        /// the TypeScript tests.
        /// </para>
        /// </summary>
        /// <param name="this">This helper.</param>
        /// <param name="testName">The current test name.</param>
        /// <returns>The TSBuildAndTests test path.</returns>
        public static NormalizedPath GetTypeScriptBuildModeTargetProjectPath( this IBasicTestHelper @this, [CallerMemberName] string? testName = null )
        {
            return @this.TestProjectFolder.AppendPart( "TSBuildAndTests" ).AppendPart( RemoveAsyncSuffix( testName ) );
        }

        enum GenerateMode
        {
            SkipTypeScriptTooling,
            BuildCKGen,
            BuildCKGenAndVSCodeSupport,
            WithTestSupport,
            BuildMode
        }

        /// <summary>
        /// Creates a <see cref="Runner"/> that MUST be disposed once <see cref="Runner.Run()"/> has been called:
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
        /// This 2-steps pattern enables to temporarily inserts a <see cref="Monitoring.IMonitorTestHelperCore.SuspendAsync"/>
        /// before calling the <see cref="Runner.Run()"/> in order to be able to keep a running context alive while
        /// working on the TypeScript side (fixing, debugging, analyzing, etc.) TypeScript code. 
        /// </para>
        /// </summary>
        /// <param name="this">This helper.</param>
        /// <param name="targetProjectPath">
        /// The test target project path. Usually obtained by:
        /// <list type="bullet">
        ///     <item><see cref="GetTypeScriptWithTestsSupportTargetProjectPath(IBasicTestHelper, string?)">TestHelper.GetTypeScriptWithTestsSupportTargetProjectPath()</see></item>
        ///     <item>or <see cref="GetTypeScriptBuildModeTargetProjectPath(IBasicTestHelper, string?)">TestHelper.GetTypeScriptBuildModeTargetProjectPath()</see></item>
        /// </list>
        /// </param>
        /// <param name="environmentVariables">Optional environment variables to set.</param>
        /// <param name="command">Yarn command that will be executed.</param>
        public static Runner CreateTypeScriptRunner( this IMonitorTestHelper @this,
                                                     NormalizedPath targetProjectPath,
                                                     Dictionary<string, string>? environmentVariables = null,
                                                     string command = "test" )
        {
            YarnHelper.PrepareJestRun( @this.Monitor, targetProjectPath, environmentVariables, out var afterRun ).Should().BeTrue();
            return new Runner( @this, targetProjectPath, environmentVariables, command, afterRun );
        }

    }
}
