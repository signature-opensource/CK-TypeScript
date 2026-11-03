using CK.Core;
using CK.TS.Angular.Engine;
using Shouldly;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.Angular.Tests;

/// <summary>
/// Copies of the Angular 20 snapshot of the <c>CK_TS_Angular</c> fixture, each in its own git repository.
/// </summary>
static class AngularUpgradeSnapshot
{
    /// <summary>
    /// Recreates <c>TSInlineTests/Upgrade_&lt;test name&gt;</c> (without the <c>_Async</c> suffix) from
    /// <c>UpgradeSnapshots/Angular20</c>, as a new git repository whose single commit holds every file.
    /// The copy's <c>.gitignore</c> also ignores the PnP files: the rules of the outer repository do not apply
    /// inside the nested one.
    /// </summary>
    /// <param name="testName">The current test name.</param>
    /// <returns>The app folder.</returns>
    internal static NormalizedPath Prepare( [CallerMemberName] string? testName = null )
    {
        Throw.CheckNotNullArgument( testName );
        if( testName.EndsWith( "_Async" ) )
        {
            testName = testName.Substring( 0, testName.Length - 6 );
        }
        var target = TestHelper.TestProjectFolder.Combine( "TSInlineTests/Upgrade_" + testName );
        if( Directory.Exists( target ) )
        {
            // Git writes its object files read-only and Directory.Delete refuses them.
            foreach( var f in Directory.EnumerateFiles( target, "*", SearchOption.AllDirectories ) )
            {
                File.SetAttributes( f, FileAttributes.Normal );
            }
            Directory.Delete( target, recursive: true );
        }
        var source = TestHelper.TestProjectFolder.Combine( "UpgradeSnapshots/Angular20" );
        foreach( var f in Directory.EnumerateFiles( source, "*", SearchOption.AllDirectories ) )
        {
            var copy = Path.Combine( target, Path.GetRelativePath( source, f ) );
            Directory.CreateDirectory( Path.GetDirectoryName( copy )! );
            File.Copy( f, copy );
        }
        File.AppendAllText( target.AppendPart( ".gitignore" ), $".pnp.*{Environment.NewLine}.yarn/install-state.gz{Environment.NewLine}" );
        Git( target, "init -q" );
        Git( target, "add -A" );
        // A CI agent may have no git identity, and a developer's signing configuration or hooks must not run.
        Git( target, "-c user.name=ck -c user.email=ck@localhost -c commit.gpgsign=false commit -q --no-verify -m snapshot" );
        return target;
    }

    static void Git( NormalizedPath folder, string arguments )
    {
        var (exitCode, timedOut, output) = AngularGitHelper.Run( folder, arguments, AngularGitHelper.GitTimeout );
        timedOut.ShouldBeFalse( $"git {arguments}" );
        exitCode.ShouldBe( 0, $"git {arguments}{Environment.NewLine}{string.Join( Environment.NewLine, output )}" );
    }
}
