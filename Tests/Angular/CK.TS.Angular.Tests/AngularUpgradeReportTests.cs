using CK.Core;
using CK.TS.Angular.Engine;
using CSemVer;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.TS.Angular.Tests;

[TestFixture]
public class AngularUpgradeReportTests
{
    const string _stash = "ck-angular-upgrade undo CK_TS_Angular 2026-10-23T10:00:00Z";

    const string _guide = "https://angular.dev/update-guide?v=20.0-22.0&l=3";

    static readonly NormalizedPath _app = "D:/Dev/MyApp/CK_TS_Angular";

    static List<string> Lines( string text ) => text.Split( '\n' ).Select( l => l.TrimEnd( '\r' ) ).ToList();

    static int Count( string text, string value ) => (text.Length - text.Replace( value, "" ).Length) / value.Length;

    static AngularUpgradeRun CreateRun( int fromMajor,
                                        IReadOnlyList<(string Package, string Name)>? optionalMigrations = null,
                                        IReadOnlyList<string>? packagesWithoutMigrations = null )
    {
        var app = new Dictionary<string, SVersionBound>
        {
            ["@angular/core"] = SVersionBound.NpmTryParse( $"^{fromMajor}.0.0" ).Result,
            ["@angular/platform-browser-dynamic"] = SVersionBound.NpmTryParse( $"^{fromMajor}.0.0" ).Result
        };
        var steps = AngularUpgradePlanner.Plan( fromMajor, 22, app, new Dictionary<string, SVersionBound>() );
        return new AngularUpgradeRun( _app,
                                      fromMajor,
                                      22,
                                      steps,
                                      optionalMigrations ?? [],
                                      packagesWithoutMigrations ?? [],
                                      new TimeSpan( 0, 3, 12 ) );
    }

    static CompileCheckResult Passed() => new( CompileCheckOutcome.Passed, [], null, [] );

    static string Summary( AngularUpgradeRun run, IReadOnlyList<string> changedFiles, CompileCheckResult compileCheck )
    {
        return AngularUpgradeReport.RenderSummary( run, changedFiles, compileCheck, _stash, excludeRootGitIgnore: false );
    }

    [Test]
    public void guide_link_uses_dot_zero_versions()
    {
        AngularUpgradeReport.GuideLink( 20, 22 ).ShouldBe( _guide );
    }

    [Test]
    public void compile_errors_are_ts_and_ng_lines()
    {
        string[] output = ["ck-gen/a.ts:1:32 - \x1b[91merror\x1b[0m\x1b[90m TS2307: \x1b[0mCannot find module 'x'.",
                           "8     <h1 [ckNotAProperty]=\"1\"></h1>",
                           "src/app/app.html:8:5 - error NG8002: Can't bind to 'ckNotAProperty' since it isn't a known property of 'h1'.",
                           "src/app/app.ts:2:1 - warning NG8107: The left side of this optional chain operation does not include 'null' or 'undefined'.",
                           "Found 2 errors."];

        AngularUpgradeReport.GetCompileErrors( output ).ShouldBe( ["ck-gen/a.ts:1:32 - error TS2307: Cannot find module 'x'.",
                                                                    "src/app/app.html:8:5 - error NG8002: Can't bind to 'ckNotAProperty' since it isn't a known property of 'h1'."] );
    }

    [Test]
    public void matching_hint_is_shown_next_to_its_error()
    {
        var rename = AngularMajorTooling.Rows[22].Hints.Single( h => h.MatchFragment == "NzToolTipModule" );
        var duplicate = new AngularUpgradeHint( "Two components share a selector.", MatchErrorCode: "NG8023" );
        var neverMatched = new AngularUpgradeHint( "A hint without a match." );
        string[] errors = ["src/app/a.ts:3:10 - error TS2724: '\"ng-zorro-antd/tooltip\"' has no exported member named 'NzToolTipModule'. Did you mean 'NzTooltipModule'?",
                           "src/app/b.html:1:1 - error NG8023: Multiple components match node with tagname b-item.",
                           "src/app/c.html:2:2 - error NG8002: Can't bind to 'x' since it isn't a known property of 'div'."];

        var text = AngularUpgradeReport.RenderCompileFailure( _app, errors, [rename, duplicate, neverMatched], _guide );
        var lines = Lines( text );

        int renamed = lines.FindIndex( l => l.Contains( "error TS2724" ) );
        int ng8023 = lines.FindIndex( l => l.Contains( "error NG8023" ) );
        int ng8002 = lines.FindIndex( l => l.Contains( "error NG8002" ) );
        renamed.ShouldBeGreaterThanOrEqualTo( 0 );
        lines[renamed + 1].ShouldContain( rename.Text );
        lines[ng8023 + 1].ShouldContain( duplicate.Text );
        lines[ng8002 + 1].ShouldNotContain( duplicate.Text );
        Count( text, duplicate.Text ).ShouldBe( 1 );
        Count( text, rename.Text ).ShouldBe( 1 );
        text.ShouldNotContain( neverMatched.Text );
    }

    [Test]
    public void error_code_hint_matches_the_whole_code_only()
    {
        var hint = new AngularUpgradeHint( "Two components share a selector.", MatchErrorCode: "NG8023" );
        string[] errors = ["src/app/a.html:1:1 - error NG80231: Some later error.",
                           "src/app/b.html:1:1 - error NG8023: Multiple components match node with tagname b-item."];

        var lines = Lines( AngularUpgradeReport.RenderCompileFailure( _app, errors, [hint], _guide ) );

        lines[lines.FindIndex( l => l.Contains( "error NG80231" ) ) + 1].ShouldNotContain( hint.Text );
        lines[lines.FindIndex( l => l.Contains( "error NG8023:" ) ) + 1].ShouldContain( hint.Text );
    }

    [Test]
    public void hint_with_a_fragment_and_a_code_needs_both()
    {
        var hint = new AngularUpgradeHint( "Import NzTooltipModule.", MatchFragment: "NzToolTipModule", MatchErrorCode: "NG8001" );
        string[] errors = ["src/app/a.ts:3:10 - error TS2724: has no exported member named 'NzToolTipModule'.",
                           "src/app/b.html:1:1 - error NG8001: 'nz-x' is not a known element.",
                           "src/app/c.html:2:2 - error NG8001: 'nz-tooltip' is not a known element: import NzToolTipModule."];

        var text = AngularUpgradeReport.RenderCompileFailure( _app, errors, [hint], _guide );
        var lines = Lines( text );

        Count( text, hint.Text ).ShouldBe( 1 );
        lines[lines.FindIndex( l => l.Contains( "import NzToolTipModule." ) ) + 1].ShouldContain( hint.Text );
    }

    [Test]
    public void compile_failure_groups_errors_by_file_and_caps_at_50_lines()
    {
        string[] files = ["src/app/a.ts", "src/app/b.ts", "src/app/c.ts"];
        var errors = Enumerable.Range( 0, 60 ).Select( i => $"{files[i % 3]}:{i + 1}:1 - error TS2304: Cannot find name 'x{i}'." ).ToList();

        var text = AngularUpgradeReport.RenderCompileFailure( _app, errors, [], _guide );
        var lines = Lines( text );

        text.ShouldContain( "60 errors" );
        text.ShouldContain( _guide );
        var shown = lines.Where( l => l.Contains( "error TS2304" ) ).ToList();
        shown.Count.ShouldBe( 50 );
        var headings = files.Select( f => lines.FindIndex( l => l.Contains( f ) && !l.Contains( "error TS" ) ) ).ToList();
        headings.ShouldAllBe( h => h >= 0 );
        headings.ShouldBeInOrder();
        for( int i = 0; i < files.Length; i++ )
        {
            int end = i + 1 < files.Length ? headings[i + 1] : lines.Count;
            lines.Skip( headings[i] + 1 ).Take( end - headings[i] - 1 )
                 .Where( l => l.Contains( "error TS" ) )
                 .ShouldAllBe( l => l.Contains( files[i] ) );
        }
    }

    [Test]
    public void generated_file_is_said_to_come_from_a_ck_package()
    {
        string[] errors = ["ck-gen/CK/Ng/Zorro/action-bar/action-bar.ts:4:10 - error TS2724: has no exported member named 'NzToolTipModule'.",
                           "src/app/a.ts:1:1 - error TS2304: Cannot find name 'x'."];

        var lines = Lines( AngularUpgradeReport.RenderCompileFailure( _app, errors, [], _guide ) );

        lines.Single( l => l.StartsWith( "ck-gen/CK/Ng/Zorro/action-bar/action-bar.ts" ) ).ShouldContain( "fix it in that package" );
        lines.Single( l => l.StartsWith( "src/app/a.ts" ) ).ShouldNotContain( "package" );
    }

    [Test]
    public void compile_failure_message_says_a_rerun_passes_without_the_check()
    {
        var text = AngularUpgradeReport.RenderCompileFailure( _app, ["src/app/a.ts:1:1 - error TS2304: Cannot find name 'x'."], [], _guide );

        text.ShouldContain( "a rerun of the builder passes without this check" );
        text.ShouldContain( "before committing" );
        text.ShouldContain( $"yarn {AngularUpgradeReport.CompileCheckCommand}" );
    }

    [Test]
    public void typescript_errors_warn_that_template_errors_can_follow()
    {
        // @angular/compiler-cli 22.2.3 defaultGatherDiagnostics: template diagnostics run only after error-free earlier stages.
        const string warning = "the Angular compiler does not check the templates";
        var withTs = AngularUpgradeReport.RenderCompileFailure( _app,
                                                                ["src/app/a.ts:1:1 - error TS2304: Cannot find name 'x'.",
                                                                 "src/app/b.ts:1:1 - error NG2009: Component selector is invalid."],
                                                                [],
                                                                _guide );
        var templatesOnly = AngularUpgradeReport.RenderCompileFailure( _app,
                                                                       ["src/app/c.html:2:2 - error NG8002: Can't bind to 'x' since it isn't a known property of 'div'."],
                                                                       [],
                                                                       _guide );

        withTs.ShouldContain( warning );
        templatesOnly.ShouldNotContain( warning );
    }

    [Test]
    public void compile_check_that_gives_no_answer_did_not_run()
    {
        string[] output = ["Compiling the app.", "Error: Cannot find module 'typescript'."];

        var exited = AngularUpgradeReport.EvaluateCompileCheck( 1, false, output );
        var timedOut = AngularUpgradeReport.EvaluateCompileCheck( 0, true, [] );

        exited.Outcome.ShouldBe( CompileCheckOutcome.DidNotRun );
        timedOut.Outcome.ShouldBe( CompileCheckOutcome.DidNotRun );
        timedOut.NotRunReason.ShouldBe( "timed out after 10 minutes" );
        var text = AngularUpgradeReport.RenderCompileCheckNotRun( _app, AngularUpgradeReport.CompileCheckCommand, exited, _guide );
        text.ShouldContain( $"yarn {AngularUpgradeReport.CompileCheckCommand}" );
        text.ShouldContain( "exit code 1" );
        text.ShouldContain( "Error: Cannot find module 'typescript'." );
        text.ShouldContain( "the compile check did not run" );
    }

    [Test]
    public void summary_lists_optional_migrations_with_their_commands()
    {
        var run = CreateRun( 20, optionalMigrations: [("@angular/cli", "use-application-builder"), ("@angular/cli", "use-application-builder")] );

        var text = Summary( run, [], Passed() );

        Count( text, "ng update @angular/cli --name use-application-builder" ).ShouldBe( 1 );
    }

    [Test]
    public void summary_includes_hints_of_crossed_rows_and_the_material_note()
    {
        var fromTwenty = Summary( CreateRun( 20 ), [], Passed() );
        var fromTwentyOne = Summary( CreateRun( 21 ), [], Passed() );

        foreach( var hint in AngularMajorTooling.Rows[21].Hints.Concat( AngularMajorTooling.Rows[22].Hints ) )
        {
            fromTwenty.ShouldContain( hint.Text );
        }
        fromTwentyOne.ShouldNotContain( AngularMajorTooling.Rows[21].Hints[0].Text );
        fromTwenty.ShouldContain( "tick 'I use Angular Material' if the app uses it" );
    }

    [Test]
    public void summary_lists_every_item_of_the_spec()
    {
        var run = CreateRun( 20, packagesWithoutMigrations: ["@angular/cdk", "@angular/cdk"] );
        string[] changed = [" M CK_TS_Angular/package.json", "?? CK_TS_Angular/src/app/new.ts"];
        var errors = new CompileCheckResult( CompileCheckOutcome.Errors, ["a", "b", "c"], null, [] );
        var notRun = new CompileCheckResult( CompileCheckOutcome.DidNotRun, [], "exit code 1", [] );

        var text = AngularUpgradeReport.RenderSummary( run, changed, Passed(), _stash, excludeRootGitIgnore: true );
        var lines = Lines( text );

        text.ShouldContain( "upgraded from Angular 20 to 22" );
        text.ShouldContain( "20 to 21" );
        text.ShouldContain( "21 to 22" );
        text.ShouldContain( "3 min 12 s" );
        text.ShouldContain( changed[0] );
        text.ShouldContain( changed[1] );
        text.ShouldContain( "Compile check: passed." );
        Count( text, "@angular/cdk" ).ShouldBe( 1 );
        text.ShouldContain( "'@angular/platform-browser-dynamic' replaced by '@angular/platform-browser'" );
        text.ShouldContain( _guide );
        int stash = lines.IndexOf( $"git stash push --include-untracked -m \"{_stash}\" -- . \":(exclude)ck-gen\" \":(exclude).yarn\" \":(exclude).yarnrc.yml\" \":(exclude).ck-angular-upgrade\" \":(exclude).gitignore\"" );
        stash.ShouldBeGreaterThan( 0 );
        lines[stash + 1].ShouldBe( "yarn install" );
        Summary( run, changed, errors ).ShouldContain( "Compile check: 3 errors." );
        Summary( run, changed, notRun ).ShouldContain( "Compile check: did not run (exit code 1)." );
    }

    [Test]
    public void integration_failure_after_upgrade_is_reported_with_the_stash()
    {
        var text = AngularUpgradeReport.RenderIntegrationFailure( _app, 20, 22, _stash, AngularUndoOutcome.Restored, stashCreated: true, excludeRootGitIgnore: false );

        text.ShouldContain( "The integration failed after the upgrade" );
        text.ShouldContain( _stash );
        text.ShouldContain( "git stash drop" );
        text.ShouldContain( "1. " );
    }
}
