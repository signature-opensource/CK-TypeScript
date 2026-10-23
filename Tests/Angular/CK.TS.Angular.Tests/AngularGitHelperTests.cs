using CK.Core;
using CK.TS.Angular.Engine;
using NUnit.Framework;
using Shouldly;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.Angular.Tests;

/// <summary>
/// The app sits in a sub folder of the repository, so the porcelain paths, relative to the repository root, differ
/// from the paths relative to the app folder.
/// </summary>
[TestFixture]
public class AngularGitHelperTests
{
    string _root = null!;
    string _app = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine( Path.GetTempPath(), "ck-angular-git-" + Guid.NewGuid().ToString( "N" ) );
        _app = Path.Combine( _root, "App" );
        Directory.CreateDirectory( Path.Combine( _app, "ck-gen" ) );
        File.WriteAllText( Path.Combine( _app, "package.json" ), "{}" );
        File.WriteAllText( Path.Combine( _root, "other.txt" ), "x" );
        Git( "init -q" );
        Git( "add -A" );
        Git( "-c user.name=ck -c user.email=ck@localhost commit -q -m init" );
    }

    [TearDown]
    public void TearDown()
    {
        // Git writes its object files read-only and Directory.Delete refuses them.
        foreach( var f in Directory.EnumerateFiles( _root, "*", SearchOption.AllDirectories ) )
        {
            File.SetAttributes( f, FileAttributes.Normal );
        }
        Directory.Delete( _root, recursive: true );
    }

    void Git( string args ) => GitOutput( args );

    string GitOutput( string args )
    {
        var info = new ProcessStartInfo( "git", args ) { WorkingDirectory = _root, UseShellExecute = false, RedirectStandardOutput = true };
        using var p = Process.Start( info )!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        p.ExitCode.ShouldBe( 0 );
        return output;
    }

    [Test]
    public void modified_and_untracked_files_in_the_app_are_dirty()
    {
        File.WriteAllText( Path.Combine( _app, "package.json" ), """{"name":"app"}""" );
        File.WriteAllText( Path.Combine( _app, "new.ts" ), "export {}" );

        var dirty = AngularGitHelper.GetDirtyFiles( TestHelper.Monitor, _app );

        dirty.ShouldNotBeNull();
        dirty.Count.ShouldBe( 2 );
        dirty.ShouldContain( " M App/package.json" );
        dirty.ShouldContain( "?? App/new.ts" );
    }

    [Test]
    public void untracked_files_are_dirty_even_when_the_repository_hides_them_from_status()
    {
        Git( "config status.showUntrackedFiles no" );
        File.WriteAllText( Path.Combine( _app, "new.ts" ), "export {}" );

        AngularGitHelper.GetDirtyFiles( TestHelper.Monitor, _app ).ShouldBe( new[] { "?? App/new.ts" } );
    }

    [Test]
    public void output_lines_that_are_not_porcelain_entries_are_warned_and_not_dirty()
    {
        var changes = new[] { " M App/package.json",
                              "MT App/a.ts",
                              "A  App/b.ts",
                              " D App/c.ts",
                              "R  App/d.ts -> App/e.ts",
                              "C  App/f.ts -> App/g.ts",
                              "UU App/h.ts",
                              "?? App/new.ts",
                              "!! App/i.ts" };
        var lines = changes.Concat( new[] { "warning: in the working copy of 'App/package.json', LF will be replaced by CRLF",
                                            "?? App/.gitignore",
                                            "" } );
        using( TestHelper.Monitor.CollectEntries( out var entries, LogLevelFilter.Warn ) )
        {
            AngularGitHelper.GetPorcelainEntries( TestHelper.Monitor, _app, lines, "?? App/.gitignore" )
                            .ShouldBe( changes );
            entries.Count.ShouldBe( 1 );
            entries[0].Text.ShouldContain( new NormalizedPath( _app ).Path );
            entries[0].Text.ShouldContain( "LF will be replaced by CRLF" );
        }
    }

    [Test]
    public void changes_outside_the_app_under_ck_gen_or_to_yarn_files_are_ignored()
    {
        File.WriteAllText( Path.Combine( _root, "other.txt" ), "y" );
        File.WriteAllText( Path.Combine( _app, "ck-gen", "gen.ts" ), "export {}" );
        Directory.CreateDirectory( Path.Combine( _app, ".yarn", "releases" ) );
        File.WriteAllText( Path.Combine( _app, ".yarn", "releases", "yarn-4.8.1.cjs" ), "//" );
        File.WriteAllText( Path.Combine( _app, ".yarnrc.yml" ), "nodeLinker: pnp" );

        AngularGitHelper.GetDirtyFiles( TestHelper.Monitor, _app ).ShouldBeEmpty();
    }

    [Test]
    public void folder_outside_git_returns_null()
    {
        var outside = Path.Combine( Path.GetTempPath(), "ck-angular-nogit-" + Guid.NewGuid().ToString( "N" ) );
        Directory.CreateDirectory( outside );
        try
        {
            AngularGitHelper.GetDirtyFiles( TestHelper.Monitor, outside ).ShouldBeNull();
        }
        finally
        {
            Directory.Delete( outside );
        }
    }

    [Test]
    public void untracked_root_gitignore_is_not_dirty()
    {
        File.WriteAllText( Path.Combine( _app, ".gitignore" ), ".pnp.cjs" );

        AngularGitHelper.GetDirtyFiles( TestHelper.Monitor, _app ).ShouldBeEmpty();
    }

    [Test]
    public void modified_tracked_gitignore_is_dirty()
    {
        File.WriteAllText( Path.Combine( _app, ".gitignore" ), ".pnp.cjs" );
        Git( "add -- App/.gitignore" );
        Git( "-c user.name=ck -c user.email=ck@localhost commit -q -m gitignore" );
        File.WriteAllText( Path.Combine( _app, ".gitignore" ), ".pnp.cjs\n.pnp.loader.mjs" );

        AngularGitHelper.GetDirtyFiles( TestHelper.Monitor, _app ).ShouldBe( new[] { " M App/.gitignore" } );
    }

    [Test]
    public void stash_undo_restores_tracked_and_untracked_files_and_keeps_them_in_a_stash()
    {
        File.WriteAllText( Path.Combine( _app, "package.json" ), """{"name":"app"}""" );
        Directory.CreateDirectory( Path.Combine( _app, "src" ) );
        File.WriteAllText( Path.Combine( _app, "src", "new.ts" ), "export {}" );

        AngularGitHelper.StashUndo( TestHelper.Monitor, _app, "ck-angular-upgrade undo App 2026-10-23T10:00:00Z" ).ShouldBeTrue();

        File.ReadAllText( Path.Combine( _app, "package.json" ) ).ShouldBe( "{}" );
        File.Exists( Path.Combine( _app, "src", "new.ts" ) ).ShouldBeFalse();
        GitOutput( "stash list" ).ShouldContain( "ck-angular-upgrade undo App 2026-10-23T10:00:00Z" );
        var stashed = GitOutput( "stash show --include-untracked --name-only stash@{0}" );
        stashed.ShouldContain( "App/package.json" );
        stashed.ShouldContain( "App/src/new.ts" );
    }

    [Test]
    public void stash_undo_leaves_excluded_paths_untouched()
    {
        File.WriteAllText( Path.Combine( _app, "package.json" ), """{"name":"app"}""" );
        File.WriteAllText( Path.Combine( _app, "ck-gen", "gen.ts" ), "export {}" );

        AngularGitHelper.StashUndo( TestHelper.Monitor, _app, "ck-angular-upgrade undo App 2026-10-23T10:00:00Z" ).ShouldBeTrue();

        File.ReadAllText( Path.Combine( _app, "ck-gen", "gen.ts" ) ).ShouldBe( "export {}" );
    }

    [Test]
    public void stash_undo_keeps_an_untracked_root_gitignore()
    {
        File.WriteAllText( Path.Combine( _app, ".gitignore" ), ".pnp.cjs" );
        File.WriteAllText( Path.Combine( _app, "package.json" ), """{"name":"app"}""" );

        AngularGitHelper.StashUndo( TestHelper.Monitor, _app, "ck-angular-upgrade undo App 2026-10-23T10:00:00Z" ).ShouldBeTrue();

        File.ReadAllText( Path.Combine( _app, ".gitignore" ) ).ShouldBe( ".pnp.cjs" );
        File.ReadAllText( Path.Combine( _app, "package.json" ) ).ShouldBe( "{}" );
    }

    [Test]
    public void git_call_that_does_not_answer_is_cut_at_the_timeout()
    {
        var watch = Stopwatch.StartNew();

        var (_, timedOut, output) = AngularGitHelper.Run( _root,
                                                          "-e \"console.log(process.pid); setTimeout(()=>{}, 60000)\"",
                                                          TimeSpan.FromSeconds( 2 ),
                                                          fileName: "node" );

        watch.Elapsed.ShouldBeLessThan( TimeSpan.FromSeconds( 30 ) );
        timedOut.ShouldBeTrue();
        // The pid printed before the kill proves the output read so far is kept, and identifies the killed process.
        output.Count.ShouldBe( 1 );
        IsRunning( int.Parse( output[0] ) ).ShouldBeFalse();
    }

    [Test]
    public void output_held_open_by_a_process_that_left_the_tree_does_not_block_the_call()
    {
        // The process starts a detached child that inherits its output, prints the child's pid and exits at once.
        // Detached, because node on Windows kills its other children when it exits.
        var script = "const c = require('child_process').spawn(process.execPath, ['-e', 'setTimeout(()=>{}, 40000)'], { detached: true, stdio: 'inherit', windowsHide: true }); console.log(c.pid); c.unref();";
        var watch = Stopwatch.StartNew();

        var (exitCode, timedOut, output) = AngularGitHelper.Run( _root, $"-e \"{script}\"", TimeSpan.FromSeconds( 20 ), fileName: "node" );

        try
        {
            watch.Elapsed.ShouldBeLessThan( TimeSpan.FromSeconds( 15 ) );
            timedOut.ShouldBeFalse();
            exitCode.ShouldBe( 0 );
            output.Count.ShouldBe( 1 );
        }
        finally
        {
            if( output.Count > 0 && int.TryParse( output[0], out var childId ) )
            {
                KillIfRunningNode( childId );
            }
        }
    }

    static void KillIfRunningNode( int processId )
    {
        if( IsRunning( processId ) )
        {
            using var process = Process.GetProcessById( processId );
            // The id may have been reused by an unrelated process.
            if( process.ProcessName == "node" )
            {
                process.Kill();
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
}
