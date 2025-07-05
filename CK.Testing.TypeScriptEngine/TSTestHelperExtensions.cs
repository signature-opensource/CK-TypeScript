using CK.Core;
using CK.Setup;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace CK.Testing;

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
    /// <param name="helper">This helper.</param>
    /// <param name="testName">The current test name.</param>
    /// <returns>The TSGeneratedOnly test path.</returns>
    public static NormalizedPath GetTypeScriptGeneratedOnlyTargetProjectPath( this IMonitorTestHelper helper, [CallerMemberName] string? testName = null )
    {
        var p = helper.TestProjectFolder.AppendPart( "TSGeneratedOnly" );
        EnsureGitIgnore( helper, p );
        return p.AppendPart( RemoveAsyncSuffix( testName ) );
    }

    static void EnsureGitIgnore( IMonitorTestHelper helper, NormalizedPath p )
    {
        if( !Directory.Exists( p ) )
        {
            helper.Monitor.Info( $"Creating folder with .gitignore \"*\": '{p}'." );
            Directory.CreateDirectory( p );
            File.WriteAllText( p.AppendPart( ".gitignore" ), "*" );
        }
    }

    static string RemoveAsyncSuffix( string? testName )
    {
        Throw.DebugAssert( testName != null );
        if( testName.EndsWith( "_Async" ) ) testName = testName.Substring( 0, testName.Length - 6 );
        return testName;
    }

    /// <summary>
    /// Gets "<see cref="IBasicTestHelper.TestProjectFolder"/>/TSInlineTests/<paramref name="testName"/>" path
    /// for real tests. Yarn is installed, VSCode support is setup, a script "test" command is
    /// available and a "src/sample.spec.ts" file is ready to be used.
    /// <para>
    /// <see cref="CreateTypeScriptRunner(IMonitorTestHelper, NormalizedPath, string?, Dictionary{string, string}?, string)"/> can
    /// be used to execute the TypeScript tests.
    /// </para>
    /// </summary>
    /// <param name="this">This helper.</param>
    /// <param name="testName">The current test name.</param>
    /// <returns>The NpmPackageTests test path.</returns>
    public static NormalizedPath GetTypeScriptInlineTargetProjectPath( this IBasicTestHelper @this, [CallerMemberName] string? testName = null )
    {
        var p = GetPath( @this, "TSInlineTests", testName );
        MigrateAny( @this, testName, p, "TSBuildOnly", "TSGeneratedOnly" );
        return p;
    }

    static void MigrateAny( IBasicTestHelper @this, string? testName, NormalizedPath p, params string[] others )
    {
        foreach( var o in others )
        {
            if( MoveDirectory( GetPath( @this, o, testName ), p ) )
            {
                return;
            }
        }
    }

    static NormalizedPath GetPath( IBasicTestHelper @this, string type, string? testName ) => @this.TestProjectFolder.AppendPart( type ).AppendPart( RemoveAsyncSuffix( testName ) );

    static bool MoveDirectory( NormalizedPath old, NormalizedPath p )
    {
        if( Directory.Exists( old ) )
        {
            try
            {
                Directory.CreateDirectory( p.RemoveLastPart() );
                Directory.Move( old, p );
            }
            catch( Exception ex )
            {
                throw new Exception( $"While moving previous '{old}' to new {p}.", ex );
            }
            return true;
        }
        return false;
    }

    enum GenerateMode
    {
        GenerateOnly,
        Inline
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
    /// <para>
    /// When <paramref name="serverAddress"/> is not null the tests are configured to be hosted on the server address:
    /// the <c>window.location.origin</c> is the server address.
    /// </para>
    /// This also modifies the <c>ck-gen/CK/Angular/CKGenAppModule.ts</c> file
    /// if it exists to inject the server address:
    /// <list type="number">
    ///     <item>In the first <c>new AuthService( inject( AXIOS ), { identityEndPoint: 'serverAddress' } )</c>.</item>
    ///     <item>In the first <c>new HttpCrisEndpoint( inject( AXIOS ), 'serverAddress' )</c>.</item>
    /// </list>
    /// This (not really elegant - this package doesn't know CK.NG.AspNet.Auth nor CK.Ng.Cris.AspNet) trick enables
    /// 'yarn start' when the backend is running to launch a fully operational application.
    /// </summary>
    /// <param name="this">This helper.</param>
    /// <param name="targetProjectPath">
    /// The test target project path. Should be obtained by:
    /// <list type="bullet">
    ///     <item><see cref="GetTypeScriptInlineTargetProjectPath(IBasicTestHelper, string?)">TestHelper.GetTypeScriptInlineTargetProjectPath()</see></item>
    /// </list>
    /// </param>
    /// <param name="serverAddress">Optional server address that will replace the default "http://localhost".</param>
    /// <param name="environmentVariables">Optional environment variables to set (will apeear in CKTypeScriptEnv).</param>
    /// <param name="command">Yarn command that will be executed.</param>
    public static Runner CreateTypeScriptRunner( this IMonitorTestHelper @this,
                                                 NormalizedPath targetProjectPath,
                                                 string? serverAddress = null,
                                                 Dictionary<string, string>? environmentVariables = null,
                                                 string command = "test" )
    {
        TypeScriptIntegrationContext.JestSetupHandler.PrepareJestRun( @this.Monitor,
                                                                      targetProjectPath,
                                                                      out var afterRun,
                                                                      serverAddress,
                                                                      environmentVariables ).ShouldBeTrue();
        Action<IActivityMonitor>? revert = serverAddress != null
                                            ? SetServerAddressInCKGenAppModule( @this.Monitor, targetProjectPath, serverAddress )
                                            : null;
        Action<IActivityMonitor>? final = afterRun;
        if( revert != null )
        {
            final = afterRun == null
                        ? revert
                        : (monitor => { revert( monitor ); afterRun( monitor ); });

        }
        return new Runner( @this, targetProjectPath, environmentVariables, command, final );

        static Action<IActivityMonitor>? SetServerAddressInCKGenAppModule( IActivityMonitor monitor,
                                                                           NormalizedPath targetProjectPath,
                                                                           string serverAddress )
        {
            var ckGenAppModulePath = targetProjectPath.Combine( "ck-gen/CK/Angular/CKGenAppModule.ts" );
            if( File.Exists( ckGenAppModulePath ) )
            {
                bool changed = false;
                var originText = File.ReadAllText( ckGenAppModulePath );
                var text = originText;
                var idx = text.IndexOf( "new AuthService( inject( AXIOS ) )" );
                if( idx > 0 )
                {
                    Throw.DebugAssert( "new AuthService( inject( AXIOS )".Length == 32 );
                    text = text.Insert( idx + 32, $$""", { identityEndPoint: '{{serverAddress}}' }""" );
                    monitor.Info( "Added authentication endpoint to new AuthService in 'CKGenAppModule.ts' file." );
                    changed = true;
                }
                idx = text.IndexOf( "new HttpCrisEndpoint( inject( AXIOS ) )" );
                if( idx > 0 )
                {
                    Throw.DebugAssert( "new HttpCrisEndpoint( inject( AXIOS )".Length == 37 );
                    text = text.Insert( idx + 37, $$""", '{{serverAddress}}/.cris'""" );
                    monitor.Info( "Added cris endpoint to new HttpCrisEndpoint in 'CKGenAppModule.ts' file." );
                    changed = true;
                }
                if( changed )
                {
                    File.WriteAllText( ckGenAppModulePath, text );
                    return monitor => File.WriteAllText( ckGenAppModulePath, originText );
                }
            }
            return null;
        }
    }

}
