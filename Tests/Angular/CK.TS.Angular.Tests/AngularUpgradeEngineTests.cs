using CK.Core;
using CK.Setup;
using CK.Testing;
using CK.TS.Angular.Engine;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.Angular.Tests;

/// <summary>
/// The guards of the Angular upgrade, run by the engine on a copy of the Angular 20 snapshot. Every test stops
/// before the first <c>ng update</c>.
/// </summary>
[TestFixture]
public class AngularUpgradeEngineTests
{
    [Test]
    public async Task dirty_app_fails_untouched_Async()
    {
        var app = AngularUpgradeSnapshot.Prepare();
        var packageJson = File.ReadAllBytes( app.AppendPart( "package.json" ) );
        File.WriteAllText( app.Combine( "src/app/wip.ts" ), "export const wip = 1;" );

        var run = await RunEngineAsync( app );

        run.Status.ShouldBe( RunStatus.Failed );
        run.Errors.ShouldContain( e => e.Text.Contains( "uncommitted change" ) && e.Text.Contains( "src/app/wip.ts" ) );
        File.ReadAllBytes( app.AppendPart( "package.json" ) ).ShouldBe( packageJson );
        File.Exists( app.AppendPart( AngularUpgradeMarker.FileName ) ).ShouldBeFalse();
    }

    [Test]
    public async Task ck_package_on_old_major_fails_Async()
    {
        var app = AngularUpgradeSnapshot.Prepare();

        var run = await RunEngineAsync( app, typeof( NotReadyZorroPackage ) );

        run.Status.ShouldBe( RunStatus.Failed );
        run.Errors.ShouldContain( e => e.Text.Contains( "ng-zorro-antd" )
                                   && e.Text.Contains( "^20" )
                                   && e.Text.Contains( "CK.TS.Angular.Tests.NotReadyZorroPackage" ) );
    }

    [Test]
    public async Task app_below_first_major_fails_Async()
    {
        var app = AngularUpgradeSnapshot.Prepare();
        SetAngularCore( app, "^19.2.0" );
        Git( app, "-c user.name=ck -c user.email=ck@localhost -c commit.gpgsign=false commit -q --no-verify -am app19" );

        var run = await RunEngineAsync( app );

        run.Status.ShouldBe( RunStatus.Failed );
        run.Errors.ShouldContain( e => e.Text.Contains( "Angular 20" ) );
        // The planner has no row for Angular 20: it must not run when the first condition fails.
        run.Errors.ShouldNotContain( e => e.Exception != null || e.Text.Contains( "While raising AfterCodeGeneration" ) );
    }

    [TestCase( true )]
    [TestCase( false )]
    public async Task killed_run_is_undone_first_Async( bool readableMarker )
    {
        var app = AngularUpgradeSnapshot.Prepare();
        var snapshotPackageJson = File.ReadAllText( TestHelper.TestProjectFolder.Combine( "UpgradeSnapshots/Angular20/package.json" ) );
        WriteMarker( app, readableMarker );
        // A complete upgrade that the compile check never verified.
        SetAngularCore( app, "^22.0.0" );
        File.WriteAllText( app.Combine( "src/app/half.ts" ), "export const half = 1;" );

        // The package is not ready for Angular 22: the run stops at the safety check of the restored app.
        var run = await RunEngineAsync( app, typeof( NotReadyZorroPackage ) );

        run.Status.ShouldBe( RunStatus.Failed );
        Git( app, "stash list" ).ShouldContain( l => l.Contains( "ck-angular-upgrade undo" ) );
        // Git checks the file out with the line endings of its core.autocrlf setting.
        File.ReadAllText( app.AppendPart( "package.json" ) ).ReplaceLineEndings().ShouldBe( snapshotPackageJson.ReplaceLineEndings() );
        File.Exists( app.Combine( "src/app/half.ts" ) ).ShouldBeFalse();
        File.Exists( app.AppendPart( AngularUpgradeMarker.FileName ) ).ShouldBeFalse();
        run.Texts.ShouldContain( t => t.Contains( "not verified" ) && t.Contains( "two builds" ) );
        run.Errors.ShouldContain( e => e.Text.Contains( "CK.TS.Angular.Tests.NotReadyZorroPackage" ) );
        run.Texts.ShouldNotContain( t => t.Contains( "half-done upgrade was committed" ) );
    }

    [Test]
    public async Task committed_partial_upgrade_fails_Async()
    {
        var app = AngularUpgradeSnapshot.Prepare();
        SetAngularCore( app, "^21.0.0" );
        Git( app, "-c user.name=ck -c user.email=ck@localhost -c commit.gpgsign=false commit -q --no-verify -am app21" );
        WriteMarker( app, readable: true );

        var run = await RunEngineAsync( app );

        run.Status.ShouldBe( RunStatus.Failed );
        run.Errors.ShouldContain( e => e.Text.Contains( "half-done upgrade was committed" ) );
        File.Exists( app.AppendPart( AngularUpgradeMarker.FileName ) ).ShouldBeFalse();
        // The developer reverts the commit and reruns the builder, which installs: this run does not.
        File.Exists( app.AppendPart( ".pnp.cjs" ) ).ShouldBeFalse();
    }

    [Test]
    public async Task recovery_that_cannot_stash_fails_Async()
    {
        var app = AngularUpgradeSnapshot.Prepare();
        WriteMarker( app, readable: true );
        File.WriteAllText( app.Combine( "src/app/half.ts" ), "export const half = 1;" );
        // Git cannot write its index while another process holds the lock: 'git stash push' fails.
        var indexLock = app.Combine( ".git/index.lock" );
        File.WriteAllText( indexLock, "" );
        EngineRun run;
        try
        {
            run = await RunEngineAsync( app );
        }
        finally
        {
            File.Delete( indexLock );
        }

        run.Status.ShouldBe( RunStatus.Failed );
        // The commands to run by hand: the stash (same message prefix as the attempted one), the install, the marker deletion.
        run.Errors.ShouldContain( e => e.Text.Contains( "could not be undone" )
                                       && e.Text.Contains( "git stash push --include-untracked" )
                                       && e.Text.Contains( "ck-angular-upgrade undo" )
                                       && e.Text.Contains( "yarn install" )
                                       && e.Text.Contains( $"Remove-Item {AngularUpgradeMarker.FileName}" ) );
        // The copy's .gitignore is committed: the printed stash command must not leave its changes out.
        run.Errors.ShouldNotContain( e => e.Text.Contains( "\":(exclude).gitignore\"" ) );
        File.Exists( app.AppendPart( AngularUpgradeMarker.FileName ) ).ShouldBeTrue();
        File.Exists( app.Combine( "src/app/half.ts" ) ).ShouldBeTrue();
        Git( app, "stash list" ).ShouldBeEmpty();
    }

    [Test]
    public async Task recovery_keeps_package_manager_Async()
    {
        Assume.That( ContinuousIntegrationDetector.Detect( Environment.GetEnvironmentVariable ), Is.Null, "On CI, the safety check refuses the upgrade." );
        var app = AngularUpgradeSnapshot.Prepare();
        // The engine runs the repository's yarn and sets it in memory: the committed file names another one.
        var path = app.AppendPart( "package.json" );
        var text = File.ReadAllText( path );
        text.ShouldContain( "\"packageManager\": \"yarn@4.8.1\"" );
        File.WriteAllText( path, text.Replace( "\"packageManager\": \"yarn@4.8.1\"", "\"packageManager\": \"yarn@4.0.2\"" ) );
        Git( app, "-c user.name=ck -c user.email=ck@localhost -c commit.gpgsign=false commit -q --no-verify -am yarn402" );
        WriteMarker( app, readable: true );

        // The step after the recovery saves package.json first, then fails offline: its undo stashes that save.
        var run = await RunEngineOfflineAsync( app );

        run.Status.ShouldBe( RunStatus.Failed );
        run.Errors.ShouldContain( e => e.Text.Contains( "20 to 21" ) && e.Text.Contains( "'yarn install'" ) );
        Git( app, "show stash@{0}:package.json" ).ShouldContain( l => l.Contains( $"\"packageManager\": \"yarn@{YarnHelper.AutomaticYarnVersion}\"" ) );
    }

    [Test]
    public async Task unreadable_index_excludes_gitignore_Async()
    {
        var app = AngularUpgradeSnapshot.Prepare();
        WriteMarker( app, readable: true );
        // Git cannot read its index: 'git ls-files' fails, so nothing says whether .gitignore is tracked.
        var index = app.Combine( ".git/index" );
        var saved = File.ReadAllBytes( index );
        File.WriteAllText( index, "not an index" );
        EngineRun run;
        try
        {
            run = await RunEngineAsync( app );
        }
        finally
        {
            File.WriteAllBytes( index, saved );
        }

        run.Status.ShouldBe( RunStatus.Failed );
        run.Errors.ShouldContain( e => e.Text.Contains( "could not be undone" ) && e.Text.Contains( "\":(exclude).gitignore\"" ) );
    }

    [Test]
    public async Task failed_step_is_undone_Async()
    {
        Assume.That( ContinuousIntegrationDetector.Detect( Environment.GetEnvironmentVariable ), Is.Null, "On CI, the safety check refuses the upgrade." );
        var app = AngularUpgradeSnapshot.Prepare();
        var packageJson = File.ReadAllText( app.AppendPart( "package.json" ) );
        // The first step sets jest-preset-angular ^17.0.1, absent from yarn.lock: resolving it needs the registry.
        var run = await RunEngineOfflineAsync( app );

        run.Status.ShouldBe( RunStatus.Failed );
        run.Errors.ShouldContain( e => e.Text.Contains( "20 to 21" )
                                       && e.Text.Contains( "'yarn install'" )
                                       && e.Text.Contains( "exit code 1" )
                                       && e.Text.Contains( "been restored" )
                                       && !e.Text.Contains( "not been restored" )
                                       && e.Text.Contains( "ck-angular-upgrade undo" ) );
        File.ReadAllText( app.AppendPart( "package.json" ) ).ReplaceLineEndings().ShouldBe( packageJson.ReplaceLineEndings() );
        File.Exists( app.AppendPart( AngularUpgradeMarker.FileName ) ).ShouldBeFalse();
        AngularGitHelper.GetDirtyFiles( TestHelper.Monitor, app ).ShouldBeEmpty();
        Git( app, "stash show --include-untracked --name-only stash@{0}" ).ShouldContain( "package.json" );
        // The executor stops at the failed step: a second step would undo again and leave a second stash.
        Git( app, "stash list" ).Count.ShouldBe( 1 );
    }

    [Test]
    public async Task throwing_step_is_undone_Async()
    {
        Assume.That( ContinuousIntegrationDetector.Detect( Environment.GetEnvironmentVariable ), Is.Null, "On CI, the safety check refuses the upgrade." );
        var app = AngularUpgradeSnapshot.Prepare();
        // The step saves package.json first: a read-only file makes the save throw.
        var packageJsonPath = app.AppendPart( "package.json" );
        File.SetAttributes( packageJsonPath, FileAttributes.ReadOnly );
        EngineRun run;
        try
        {
            run = await RunEngineOfflineAsync( app );
        }
        finally
        {
            File.SetAttributes( packageJsonPath, FileAttributes.Normal );
        }

        run.Status.ShouldBe( RunStatus.Failed );
        run.Errors.ShouldContain( e => e.Text.Contains( "step Angular 20 to 21" )
                                       && System.Text.RegularExpressions.Regex.IsMatch( e.Text, @"An exception stopped the step: [^\r\n]*package\.json" )
                                       && e.Text.Contains( "Next actions" ) );
        run.Errors.ShouldNotContain( e => e.Text.Contains( "While raising AfterCodeGeneration" ) );
        File.Exists( app.AppendPart( AngularUpgradeMarker.FileName ) ).ShouldBeFalse();
    }

    // Needs Node 24 and the npm registry, and takes minutes: run by hand.
    [Test]
    [Explicit]
    public async Task Upgrade_from_Angular_20_snapshot_Async()
    {
        Assume.That( ContinuousIntegrationDetector.Detect( Environment.GetEnvironmentVariable ), Is.Null, "On CI, the safety check refuses the upgrade." );
        var app = AngularUpgradeSnapshot.Prepare();
        var configuration = CreateFullAppConfiguration( app );
        // CollectTexts keeps the last 100 texts whatever its capacity argument: CollectEntries honours it.
        using( TestHelper.Monitor.CollectEntries( out var entries, LogLevelFilter.Info, 100_000 ) )
        {
            await configuration.RunSuccessfullyAsync();
            var texts = entries.Select( e => e.Text ).ToList();

            var packageJson = File.ReadAllText( app.AppendPart( "package.json" ) );
            packageJson.ShouldMatch( "\"@angular/core\": \"[~^]?22" );
            packageJson.ShouldContain( "\"jest-preset-angular\": \"^17.0.1\"" );
            packageJson.ShouldNotContain( "@angular/platform-browser-dynamic" );
            File.Exists( app.AppendPart( AngularUpgradeMarker.FileName ) ).ShouldBeFalse();
            texts.ShouldContain( t => t.Contains( "Upgrading the Angular app '" ) );
            texts.ShouldContain( t => t.Contains( "upgraded from Angular 20 to 22" ) );
            texts.ShouldContain( t => t.Contains( AngularUpgradeReport.GuideLink( 20, 22 ) ) );
        }

        await using( var runner = TestHelper.CreateTypeScriptRunner( app ) )
        {
            runner.Run();
        }

        using( TestHelper.Monitor.CollectEntries( out var entries2, LogLevelFilter.Info, 100_000 ) )
        {
            await CreateFullAppConfiguration( app ).RunSuccessfullyAsync();
            entries2.ShouldNotContain( e => e.Text.Contains( "Upgrading the Angular app '" ) );
        }
    }

    // The configuration of AngularTests.CK_TS_Angular_Async, on the given app.
    static EngineConfiguration CreateFullAppConfiguration( NormalizedPath app )
    {
        var configuration = TestHelper.CreateDefaultEngineConfiguration();
        var ts = configuration.FirstBinPath.EnsureTypeScriptConfigurationAspect( app );
        ts.ActiveCultures.Add( NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" ) );
        ts.ActiveCultures.Add( NormalizedCultureInfo.EnsureNormalizedCultureInfo( "en-GB" ) );
        configuration.FirstBinPath.Assemblies.Add( "CK.TS.Angular" );
        configuration.FirstBinPath.Types.Add( typeof( CK.MiscDemo.DemoNgModule ),
                                              typeof( CK.MiscDemo.AppRoutedComponent ),
                                              typeof( CK.Ng.PublicPage.PublicPageComponent ),
                                              typeof( CK.Ng.AspNet.Auth.LoginComponent ),
                                              typeof( CK.Ng.AspNet.Auth.PasswordLostComponent ),
                                              typeof( CK.Ng.AspNet.Auth.LogoutConfirmComponent ),
                                              typeof( CK.Ng.AspNet.Auth.LogoutResultComponent ),
                                              typeof( CK.Ng.AspNet.Auth.SomeAuthPackage ),
                                              typeof( CK.Ng.PublicSection.PublicSectionComponent ),
                                              typeof( CK.Ng.PublicSection.PublicFooterComponent ),
                                              typeof( CK.Ng.PublicSection.PublicTopbarComponent ),
                                              typeof( CK.Ng.Zorro.ZorroTestPackage ),
                                              typeof( CK.Ng.Zorro.BackOfficePackage ),
                                              typeof( CK.Ng.Zorro.ActionBarComponent ),
                                              typeof( CK.Ng.Zorro.GenericFormComponent ) );
        return configuration;
    }

    /// <summary>
    /// What a test reads from an engine run.
    /// </summary>
    /// <param name="Status">The run status.</param>
    /// <param name="Texts">The texts of every entry.</param>
    /// <param name="Errors">The error and fatal entries.</param>
    sealed record EngineRun( RunStatus Status, IReadOnlyList<string> Texts, IReadOnlyList<ActivityMonitorSimpleCollector.Entry> Errors );

    static async Task<EngineRun> RunEngineAsync( NormalizedPath app, params Type[] types )
    {
        var configuration = TestHelper.CreateDefaultEngineConfiguration();
        configuration.FirstBinPath.EnsureTypeScriptConfigurationAspect( app );
        configuration.FirstBinPath.Assemblies.Add( "CK.TS.Angular" );
        configuration.FirstBinPath.Types.Add( types );
        // A run logs thousands of entries, and CollectTexts keeps the last 100 texts whatever its capacity argument.
        using( TestHelper.Monitor.CollectEntries( out var entries, LogLevelFilter.Debug, 100_000 ) )
        using( TestHelper.Monitor.CollectEntries( out var errors, LogLevelFilter.Error, 1_000 ) )
        {
            var result = await configuration.RunAsync();
            return new EngineRun( result?.Status ?? RunStatus.Failed, entries.Select( e => e.Text ).ToList(), errors.ToList() );
        }
    }

    // Yarn cannot reach the registry: whatever the step has to resolve beyond yarn.lock and the cache fails.
    static async Task<EngineRun> RunEngineOfflineAsync( NormalizedPath app )
    {
        var previous = Environment.GetEnvironmentVariable( "YARN_ENABLE_NETWORK" );
        Environment.SetEnvironmentVariable( "YARN_ENABLE_NETWORK", "0" );
        try
        {
            return await RunEngineAsync( app );
        }
        finally
        {
            Environment.SetEnvironmentVariable( "YARN_ENABLE_NETWORK", previous );
        }
    }

    static void WriteMarker( NormalizedPath app, bool readable )
    {
        if( readable )
        {
            new AngularUpgradeMarker( 20, DateTime.UtcNow ).Write( app );
        }
        else
        {
            File.WriteAllText( app.AppendPart( AngularUpgradeMarker.FileName ), "garbage" );
        }
    }

    static void SetAngularCore( NormalizedPath app, string range )
    {
        var path = app.AppendPart( "package.json" );
        var text = File.ReadAllText( path );
        text.ShouldContain( "\"@angular/core\": \"^20\"" );
        File.WriteAllText( path, text.Replace( "\"@angular/core\": \"^20\"", $"\"@angular/core\": \"{range}\"" ) );
    }

    static List<string> Git( NormalizedPath app, string arguments )
    {
        var (exitCode, timedOut, output) = AngularGitHelper.Run( app, arguments, AngularGitHelper.GitTimeout );
        timedOut.ShouldBeFalse( $"git {arguments}" );
        exitCode.ShouldBe( 0, $"git {arguments}{Environment.NewLine}{string.Join( Environment.NewLine, output )}" );
        return output.Where( l => !string.IsNullOrWhiteSpace( l ) ).ToList();
    }
}
