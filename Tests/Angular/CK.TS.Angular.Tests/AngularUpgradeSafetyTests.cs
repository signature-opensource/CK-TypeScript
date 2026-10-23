using CK.Core;
using CK.TS.Angular.Engine;
using CK.TypeScript.CodeGen;
using CSemVer;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace CK.TS.Angular.Tests;

[TestFixture]
public class AngularUpgradeSafetyTests
{
    static SVersionBound V( string npm ) => SVersionBound.NpmTryParse( npm ).Result;

    static readonly NormalizedPath _appPath = "C:/Dev/MyApp/Client";

    const string _manualCommands = "yarn ng update @angular/core@21 @angular/cli@21 --allow-dirty";

    static readonly AngularUpgradeSafetyInput _base = new( 20,
                                                           22,
                                                           SVersion.Parse( "24.21.0" ),
                                                           [],
                                                           [new DeclaredLibrary( "ng-zorro-antd", V( "^22" ), "CK.Ng.Zorro.ZorroPackage" ),
                                                            new DeclaredLibrary( "@fortawesome/angular-fontawesome", V( "^5" ), "CK.Ng.Zorro.ZorroPackage" )],
                                                           null,
                                                           new Dictionary<string, SVersionBound>
                                                           {
                                                               { "@angular/core", V( "^20.3.0" ) },
                                                               { "ng-zorro-antd", V( "^20.0.0" ) },
                                                               { "@fortawesome/angular-fontawesome", V( "^3.0.0" ) }
                                                           },
                                                           null );

    static AngularUpgradeSafetyInput WithZorro( string version, string source )
    {
        return _base with
        {
            DeclaredLibraries = [new DeclaredLibrary( "ng-zorro-antd", V( version ), source ), _base.DeclaredLibraries[1]]
        };
    }

    // The text of the numbered next action n, up to the next one.
    static string NextAction( string message, int n )
    {
        int start = message.IndexOf( $"\n{n}. " );
        start.ShouldBeGreaterThan( 0, $"Next action {n} in:{System.Environment.NewLine}{message}" );
        int end = message.IndexOf( $"\n{n + 1}. ", start );
        return end < 0 ? message.Substring( start ) : message.Substring( start, end - start );
    }

    static int CountOf( string text, string part )
    {
        int count = 0;
        for( int i = text.IndexOf( part ); i >= 0; i = text.IndexOf( part, i + part.Length ) )
        {
            count++;
        }
        return count;
    }

    [Test]
    public void safe_input_has_no_failure()
    {
        AngularUpgradeSafety.Check( _base ).ShouldBeEmpty();
    }

    [Test]
    public void node_outside_a_crossed_row_range_fails_with_the_required_range()
    {
        var failures = AngularUpgradeSafety.Check( _base with { NodeVersion = SVersion.Parse( "22.12.0" ) } );
        var failure = failures.ShouldHaveSingleItem();
        failure.Text.ShouldContain( "22.12.0" );
        failure.Text.ShouldContain( "^22.22.3" );
        failure.Text.ShouldContain( "https://angular.dev/reference/versions" );
        failure.HandUpgradeHelps.ShouldBeFalse();
    }

    [Test]
    public void node_version_that_cannot_be_read_fails()
    {
        var failure = AngularUpgradeSafety.Check( _base with { NodeVersion = null } ).ShouldHaveSingleItem();
        failure.Text.ShouldContain( "node --version" );
        failure.HandUpgradeHelps.ShouldBeFalse();
    }

    [Test]
    public void dirty_files_are_listed_up_to_20()
    {
        var dirty = Enumerable.Range( 1, 25 ).Select( i => $"?? src/app/file{i:00}.ts" ).ToList();
        var failure = AngularUpgradeSafety.Check( _base with { DirtyFiles = dirty } ).ShouldHaveSingleItem();
        foreach( var line in dirty.Take( 20 ) )
        {
            failure.Text.ShouldContain( line );
        }
        failure.Text.ShouldNotContain( "file21.ts" );
        failure.Text.ShouldContain( "5 more" );
    }

    [Test]
    public void folder_outside_git_fails()
    {
        var failure = AngularUpgradeSafety.Check( _base with { DirtyFiles = null } ).ShouldHaveSingleItem();
        failure.Text.ShouldContain( "not in a git work tree" );
    }

    [Test]
    public void ck_package_declaring_a_previous_major_fails_naming_its_type()
    {
        var failure = AngularUpgradeSafety.Check( WithZorro( "^20", "CK.Ng.Zorro.ZorroTestPackage" ) ).ShouldHaveSingleItem();
        failure.Text.ShouldContain( "ng-zorro-antd" );
        failure.Text.ShouldContain( "^20" );
        failure.Text.ShouldContain( "CK.Ng.Zorro.ZorroTestPackage" );
        failure.HandUpgradeHelps.ShouldBeFalse();
    }

    [Test]
    public void configured_library_on_a_previous_major_points_at_the_libraryversions_entry()
    {
        var failure = AngularUpgradeSafety.Check( WithZorro( "^20", PackageDependency.ConfigurationSourceName ) ).ShouldHaveSingleItem();
        failure.Text.ShouldContain( "LibraryVersions" );
        failure.Text.ShouldContain( "ng-zorro-antd" );
        failure.Text.ShouldContain( "^20" );
        failure.Text.ShouldNotContain( PackageDependency.ConfigurationSourceName );
        failure.HandUpgradeHelps.ShouldBeFalse();
    }

    [Test]
    public void cli_pin_on_another_major_fails()
    {
        var failure = AngularUpgradeSafety.Check( _base with { ConfiguredCli = V( "^20" ) } ).ShouldHaveSingleItem();
        failure.Text.ShouldContain( "@angular/cli" );
        failure.Text.ShouldContain( "^20" );
        failure.Text.ShouldContain( "Angular 22" );
        failure.HandUpgradeHelps.ShouldBeFalse();
        AngularUpgradeSafety.Check( _base with { ConfiguredCli = V( ">=20.0.0" ) } ).ShouldHaveSingleItem().Text.ShouldContain( "@angular/cli" );
    }

    [Test]
    public void cli_pin_inside_the_target_major_passes()
    {
        AngularUpgradeSafety.Check( _base with { ConfiguredCli = V( "~22.1.0" ) } ).ShouldBeEmpty();
    }

    [Test]
    public void cli_pin_without_a_major_passes()
    {
        AngularUpgradeSafety.Check( _base with { ConfiguredCli = V( "*" ) } ).ShouldBeEmpty();
    }

    [Test]
    public void ck_bounds_that_hold_a_version_of_the_target_major_pass()
    {
        var input = _base with
        {
            DeclaredLibraries = [new DeclaredLibrary( "ng-zorro-antd", V( "~22.1.0" ), "CK.Ng.Zorro.ZorroPackage" ),
                                 new DeclaredLibrary( "@angular/cdk", V( ">=20.0.0" ), "CK.Ng.Zorro.ZorroPackage" )]
        };
        AngularUpgradeSafety.Check( input ).ShouldBeEmpty();
    }

    [Test]
    public void app_below_the_first_row_fails_naming_the_first_supported_major()
    {
        var failure = AngularUpgradeSafety.Check( _base with { AppMajor = 19 } ).ShouldHaveSingleItem();
        failure.Text.ShouldContain( "Angular 20" );
        failure.HandUpgradeHelps.ShouldBeFalse();
    }

    static readonly Dictionary<int, AngularMajorTooling> _rowsTo23 = AngularMajorTooling.Rows.Values.Append( AngularMajorTooling.Rows[22] with { Major = 23 } )
                                                                                             .ToDictionary( r => r.Major );

    static AngularUpgradeSafetyInput ThreeSteps( string fontawesome )
    {
        return _base with
        {
            TargetMajor = 23,
            DeclaredLibraries = [new DeclaredLibrary( "ng-zorro-antd", V( "^23" ), "CK.Ng.Zorro.ZorroPackage" ),
                                 new DeclaredLibrary( "@fortawesome/angular-fontawesome", V( fontawesome ), "CK.Ng.Zorro.ZorroPackage" )]
        };
    }

    [Test]
    public void three_step_path_with_an_off_numbering_ck_library_fails()
    {
        var failure = AngularUpgradeSafety.Check( ThreeSteps( "^6" ), _rowsTo23 ).ShouldHaveSingleItem();
        failure.Text.ShouldContain( "@fortawesome/angular-fontawesome" );
        failure.Text.ShouldContain( "previous CK major" );
        failure.HandUpgradeHelps.ShouldBeFalse();
    }

    [Test]
    public void three_step_path_without_an_off_numbering_library_ahead_passes()
    {
        AngularUpgradeSafety.Check( ThreeSteps( "^3" ), _rowsTo23 ).ShouldBeEmpty();
    }

    [Test]
    public void ci_run_fails_and_sends_the_developer_to_their_machine()
    {
        var failure = AngularUpgradeSafety.Check( _base with { ContinuousIntegration = "Azure Pipelines" } ).ShouldHaveSingleItem();
        failure.Text.ShouldContain( "Azure Pipelines" );
        failure.Text.ShouldContain( "never runs on CI" );
        failure.Text.ShouldContain( "Run the builder on your machine" );
        failure.Text.ShouldContain( "commit the upgraded app" );
        failure.HandUpgradeHelps.ShouldBeFalse();
    }

    [Test]
    public void all_failing_conditions_are_reported_together()
    {
        var input = _base with
        {
            NodeVersion = SVersion.Parse( "22.12.0" ),
            DirtyFiles = [" M src/app/app.ts"],
            ConfiguredCli = V( "^20" ),
            ContinuousIntegration = "Azure Pipelines"
        };
        var failures = AngularUpgradeSafety.Check( input );
        failures.Count.ShouldBe( 4 );
        failures[0].Text.ShouldContain( "22.12.0" );
        failures[1].Text.ShouldContain( "src/app/app.ts" );
        failures[1].Text.ShouldContain( "1 uncommitted change:" );
        failures[2].Text.ShouldContain( "@angular/cli" );
        failures[3].Text.ShouldContain( "Azure Pipelines" );
    }

    [Test]
    public void message_omits_manual_commands_when_a_hand_upgrade_cannot_help()
    {
        var failures = AngularUpgradeSafety.Check( WithZorro( "^20", "CK.Ng.Zorro.ZorroTestPackage" ) with { DirtyFiles = [" M src/app/app.ts"] } );
        failures.Count.ShouldBe( 2 );
        var message = AngularUpgradeSafety.RenderMessage( _appPath, 20, 22, failures, _manualCommands );
        message.ShouldNotContain( _manualCommands );
        // Issue [1] is the dirty file, issue [2] the declaring type: action 2 points at [2] and repeats no text.
        message.ShouldContain( "[2] " + failures[1].Text );
        NextAction( message, 2 ).ShouldContain( "[2]" );
        NextAction( message, 2 ).ShouldNotContain( "[1]" );
        foreach( var f in failures )
        {
            CountOf( message, f.Text ).ShouldBe( 1 );
        }
    }

    [Test]
    public void message_for_an_app_below_the_first_row_has_no_manual_commands()
    {
        var failures = AngularUpgradeSafety.Check( _base with { AppMajor = 19 } );
        var message = AngularUpgradeSafety.RenderMessage( _appPath, 19, 22, failures, null );
        message.ShouldNotContain( "ng update" );
        NextAction( message, 2 ).ShouldContain( "[1]" );
        NextAction( message, 2 ).ShouldNotContain( "hand upgrade" );
        CountOf( message, failures.ShouldHaveSingleItem().Text ).ShouldBe( 1 );
    }

    [Test]
    public void message_lists_the_three_next_actions_in_order()
    {
        var failures = AngularUpgradeSafety.Check( _base with { DirtyFiles = [" M src/app/app.ts"] } );
        var message = AngularUpgradeSafety.RenderMessage( _appPath, 20, 22, failures, _manualCommands );

        int doing = message.IndexOf( "Angular 20 to 22" );
        int found = message.IndexOf( "src/app/app.ts" );
        int state = message.IndexOf( "untouched" );
        doing.ShouldBeGreaterThanOrEqualTo( 0 );
        message.ShouldContain( _appPath.Path );
        found.ShouldBeGreaterThan( doing );
        state.ShouldBeGreaterThan( found );

        NextAction( message, 1 ).ShouldContain( "Fix the issue above, then rerun the builder (recommended)" );
        NextAction( message, 2 ).ShouldContain( "by hand" );
        NextAction( message, 2 ).ShouldContain( _manualCommands );
        NextAction( message, 3 ).ShouldContain( "Stay on the previous CK major" );
        message.IndexOf( "\n1. " ).ShouldBeGreaterThan( state );
    }
}
