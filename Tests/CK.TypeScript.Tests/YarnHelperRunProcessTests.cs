using CK.Setup;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests;

[TestFixture]
public class YarnHelperRunProcessTests
{
    const string _variableName = "CK_RUNPROCESS_TEST";

    [TearDown]
    public void ClearVariable()
    {
        Environment.SetEnvironmentVariable( _variableName, null );
    }

    [Test]
    public void a_run_that_ends_before_the_timeout_collects_both_streams()
    {
        var lines = new List<string>();
        var (exitCode, timedOut) = RunNode( "-e \"console.log('out'); console.error('err')\"", null, lines, TimeSpan.FromMinutes( 1 ) );
        timedOut.ShouldBeFalse();
        exitCode.ShouldBe( 0 );
        lines.ShouldContain( "out" );
        lines.ShouldContain( "err" );
        lines.ShouldAllBe( line => line != null );
    }

    [Test]
    public void an_inherited_variable_is_overridden_not_rejected()
    {
        Environment.SetEnvironmentVariable( _variableName, "inherited" );
        var lines = new List<string>();
        var (exitCode, _) = RunNode( $"-e \"console.log(process.env.{_variableName})\"",
                                     new Dictionary<string, string?> { [_variableName] = "set" },
                                     lines,
                                     TimeSpan.Zero );
        exitCode.ShouldBe( 0 );
        lines.ShouldContain( "set" );
    }

    [Test]
    public void a_null_value_removes_an_inherited_variable()
    {
        Environment.SetEnvironmentVariable( _variableName, "inherited" );
        var lines = new List<string>();
        var (exitCode, _) = RunNode( $"-e \"console.log(process.env.{_variableName})\"",
                                     new Dictionary<string, string?> { [_variableName] = null },
                                     lines,
                                     TimeSpan.Zero );
        exitCode.ShouldBe( 0 );
        lines.ShouldContain( "undefined" );
    }

    [Test]
    public void timeout_kills_the_process_tree_and_reports_it()
    {
        // On Windows, killing a node process alone also ends its non-detached children: the child is detached so that
        // only the tree kill ends it. It prints its pid and, like the root, sleeps past the timeout.
        var lines = new List<string>();
        var (_, timedOut) = RunNode( "-e \"require('child_process').spawn(process.execPath,['-e','console.log(process.pid);setTimeout(()=>{},60000)'],{stdio:'inherit',detached:true}); setTimeout(()=>{},60000)\"",
                                     null,
                                     lines,
                                     TimeSpan.FromSeconds( 2 ) );
        timedOut.ShouldBeTrue();
        int childId = int.Parse( lines.ShouldHaveSingleItem() );
        IsRunning( childId ).ShouldBeFalse();
    }

    [Test]
    public void a_timed_run_collects_late_output_within_a_bound()
    {
        // The root prints the pid of a detached child and exits at once. The child writes "late" after 1 second
        // and holds the redirected streams for 40 seconds.
        var lines = new List<string>();
        var stopwatch = Stopwatch.StartNew();
        var (exitCode, _) = RunNode( "-e \"const c=require('child_process').spawn(process.execPath,['-e','setTimeout(()=>console.log(`late`),1000);setTimeout(()=>{},40000)'],{stdio:'inherit',detached:true}); c.unref(); console.log(c.pid)\"",
                                     null,
                                     lines,
                                     TimeSpan.FromMinutes( 1 ) );
        stopwatch.Stop();
        try
        {
            exitCode.ShouldBe( 0 );
            lines.ShouldContain( "late" );
            stopwatch.Elapsed.ShouldBeLessThan( TimeSpan.FromSeconds( 30 ) );
        }
        finally
        {
            // The child holds output handles inherited from the test host: left running, it delays the end of the test run output.
            foreach( var line in lines )
            {
                if( int.TryParse( line, out int processId ) )
                {
                    KillIfRunning( processId );
                }
            }
        }
    }

    static bool IsRunning( int processId )
    {
        try
        {
            using var process = Process.GetProcessById( processId );
            return !process.HasExited;
        }
        catch( ArgumentException )
        {
            return false;
        }
    }

    static void KillIfRunning( int processId )
    {
        if( IsRunning( processId ) )
        {
            using var process = Process.GetProcessById( processId );
            // The id may have been reused by an unrelated process.
            if( process.ProcessName == "node" ) process.Kill();
        }
    }

    static (int ExitCode, bool TimedOut) RunNode( string arguments,
                                                  Dictionary<string, string?>? environmentVariables,
                                                  List<string> outputLines,
                                                  TimeSpan timeout )
    {
        return YarnHelper.RunProcess( TestHelper.Monitor.ParallelLogger,
                                      "node",
                                      arguments,
                                      Path.GetTempPath(),
                                      environmentVariables,
                                      outputLines,
                                      timeout,
                                      out _ );
    }
}
