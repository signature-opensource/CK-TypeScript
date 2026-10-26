using CK.TS.Angular.Engine;
using CSemVer;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace CK.TS.Angular.Tests;

[TestFixture]
public class AngularUpgradePlannerTests
{
    static SVersionBound V( string npm ) => SVersionBound.NpmTryParse( npm ).Result;

    static Dictionary<string, SVersionBound> D( params (string N, string V)[] deps ) => deps.ToDictionary( d => d.N, d => V( d.V ) );

    static readonly Dictionary<string, SVersionBound> _zorroApp20 = D( ("@angular/core", "^20.3.0"),
                                                                       ("@angular/cli", "^20.3.0"),
                                                                       ("ng-zorro-antd", "^20.0.0"),
                                                                       ("@angular/cdk", "^20.0.0"),
                                                                       ("@fortawesome/angular-fontawesome", "^3.0.0"),
                                                                       ("axios", "^1.7.2"),
                                                                       ("zone.js", "~0.15.0"),
                                                                       ("jest-preset-angular", "^15.0.0"),
                                                                       ("@angular/platform-browser-dynamic", "^20.3.0") );

    static readonly Dictionary<string, SVersionBound> _declared22 = D( ("ng-zorro-antd", "^22"),
                                                                      ("@angular/cdk", "^22"),
                                                                      ("@fortawesome/angular-fontawesome", "^5"),
                                                                      ("axios", "^1.7.2"),
                                                                      ("@angular/core", "*") );

    [Test]
    public void default_cli_major_has_a_tooling_row()
    {
        AngularMajorTooling.Rows.ShouldContainKey( V( AngularCodeGeneratorImpl.DefaultAngularCliVersion ).Base.Major );
    }

    [Test]
    public void from_20_to_22_runs_two_steps_and_adds_fontawesome_only_at_the_last()
    {
        var steps = AngularUpgradePlanner.Plan( 20, 22, _zorroApp20, _declared22 );
        steps.Select( s => (s.FromMajor, s.ToMajor) ).ShouldBe( [(20, 21), (21, 22)] );
        steps[0].PackageCommand.ShouldBe( "ng update @angular/core@21 @angular/cli@21 ng-zorro-antd@21 @angular/cdk@21 --allow-dirty" );
        steps[1].PackageCommand.ShouldBe( "ng update @angular/core@22 @angular/cli@22 ng-zorro-antd@22 @angular/cdk@22 @fortawesome/angular-fontawesome@5 --allow-dirty" );
        var fontawesome = steps[1].Packages.Single( p => p.Name == "@fortawesome/angular-fontawesome" );
        fontawesome.FromMajor.ShouldBe( 3 );
        fontawesome.ToMajor.ShouldBe( 5 );
        steps[1].ReplacedPackages.ShouldBe( [("@angular/platform-browser-dynamic", "@angular/platform-browser")] );
    }

    [Test]
    public void core_group_packages_are_pinned_in_the_package_command()
    {
        var app = D( ("@angular/core", "^20"),
                     ("@angular/cli", "^20"),
                     ("@angular/cdk", "^20"),
                     ("@angular/common", "^20"),
                     ("@angular/platform-browser", "^20"),
                     ("@angular/compiler-cli", "^20"),
                     ("@angular/animations", "^20"),
                     ("@angular/platform-browser-dynamic", "^20"),
                     ("@angular/build", "^20") );
        var step = AngularUpgradePlanner.Plan( 20, 21, app, _declared22 )[0];
        step.PackageCommand.ShouldBe( "ng update @angular/core@21 @angular/cli@21 @angular/cdk@21 @angular/common@21 @angular/compiler-cli@21 @angular/platform-browser@21 --allow-dirty" );
    }

    [Test]
    public void from_21_to_22_runs_one_step_with_fontawesome()
    {
        var app = D( ("@angular/core", "^21.1.0"),
                     ("@angular/cli", "^21.1.0"),
                     ("ng-zorro-antd", "^21.0.0"),
                     ("@fortawesome/angular-fontawesome", "^4.0.0") );
        var steps = AngularUpgradePlanner.Plan( 21, 22, app, _declared22 );
        steps.Select( s => (s.FromMajor, s.ToMajor) ).ShouldBe( [(21, 22)] );
        steps[0].PackageCommand.ShouldEndWith( "@fortawesome/angular-fontawesome@5 --allow-dirty" );
    }

    [Test]
    public void migration_from_is_the_given_installed_version()
    {
        var row = new AngularMajorTooling( 22, [], V( "^22.0.0" ), [], [], [] );
        var core = new AngularUpgradePackage( "@angular/core", 21, 22 );
        var step = new AngularUpgradeStep( 21, 22, row, [core], [], [], [] );
        step.MigrationCommand( core, "21.2.27" ).ShouldBe( "ng update @angular/core --migrate-only --from 21.2.27 --to 22 --allow-dirty" );
    }

    [Test]
    public void cli_migrates_before_core_in_a_step()
    {
        var step = AngularUpgradePlanner.Plan( 20, 22, _zorroApp20, _declared22 )[0];
        step.Packages.Select( p => p.Name ).Take( 2 ).ShouldBe( ["@angular/core", "@angular/cli"] );

        AngularUpgradePlanner.GetMigrationOrder( step ).Select( p => p.Name ).Take( 2 ).ShouldBe( ["@angular/cli", "@angular/core"] );
    }

    [TestCase( "^20" )]
    [TestCase( "~20.3.0" )]
    [TestCase( "20.3.4" )]
    [TestCase( ">=20.1.0" )]
    public void app_major_is_read_from_any_range_form( string core )
    {
        AngularUpgradePlanner.GetAppMajor( D( ("@angular/core", core) ) ).ShouldBe( 20 );
    }

    [TestCase( "All" )]
    [TestCase( "None" )]
    [TestCase( "missing" )]
    public void unbounded_or_missing_core_has_no_app_major( string core )
    {
        var app = new Dictionary<string, SVersionBound> { { "@angular/cli", V( "^20.3.0" ) } };
        if( core == "All" )
        {
            app.Add( "@angular/core", SVersionBound.All );
        }
        else if( core == "None" )
        {
            app.Add( "@angular/core", SVersionBound.None );
        }
        AngularUpgradePlanner.GetAppMajor( app ).ShouldBeNull();
    }

    [Test]
    public void app_without_zorro_or_cdk_does_not_update_them()
    {
        var app = D( ("@angular/core", "^20.3.0"), ("@angular/cli", "^20.3.0") );
        var steps = AngularUpgradePlanner.Plan( 20, 22, app, _declared22 );
        steps.Count.ShouldBe( 2 );
        steps[1].PackageCommand.ShouldBe( "ng update @angular/core@22 @angular/cli@22 --allow-dirty" );
    }

    [Test]
    public void declared_library_absent_from_app_or_declared_as_any_is_ignored()
    {
        var declared = D( ("new-lib", "^4"), ("free-lib", "*") );
        var app = D( ("@angular/core", "^20.3.0"), ("@angular/cli", "^20.3.0"), ("free-lib", "^1.0.0") );
        var steps = AngularUpgradePlanner.Plan( 20, 22, app, declared );
        steps.Count.ShouldBe( 2 );
        steps[1].Packages.Select( p => p.Name ).ShouldBe( ["@angular/core", "@angular/cli"] );
    }

    [TestCase( 22 )]
    [TestCase( 23 )]
    public void app_at_or_above_target_has_no_step( int appMajor )
    {
        AngularUpgradePlanner.Plan( appMajor, 22, _zorroApp20, _declared22 ).ShouldBeEmpty();
    }

    [Test]
    public void only_angular_packages_are_replaced()
    {
        AngularMajorTooling.Rows.Values.SelectMany( r => r.ReplacedPackages )
                                       .ShouldAllBe( p => p.Package.StartsWith( "@angular/" ) && p.Replacement.StartsWith( "@angular/" ) );
    }

    [Test]
    public void manual_commands_use_installed_versions_for_the_first_step_and_name_the_source_for_later_steps()
    {
        var steps = AngularUpgradePlanner.Plan( 20, 22, _zorroApp20, _declared22 );
        var installed = new Dictionary<string, SVersion> { { "@angular/core", SVersion.Parse( "20.3.15" ) } };
        var text = AngularUpgradePlanner.RenderManualCommands( steps, installed );

        text.ShouldContain( "--from 20.3.15 --to 21" );
        text.ShouldContain( "ng update @angular/core --migrate-only --from <version printed by the previous ng update for @angular/core> --to 22" );
        text.ShouldContain( "$env:NG_DISABLE_VERSION_CHECK = \"1\"" );
        text.ShouldContain( "$env:NG_FORCE_TTY = \"0\"" );
        text.ShouldContain( "\"jest-preset-angular\": \"^17.0.1\"" );
        text.ShouldContain( "\"zone.js\": \"~0.15.0\"" );
        text.ShouldContain( "yarn remove @angular/platform-browser-dynamic" );

        var lines = text.Split( '\n' ).Select( l => l.TrimEnd( '\r' ) ).ToList();
        var packageCommands = lines.Select( ( l, i ) => (l, i) ).Where( x => x.l.StartsWith( "yarn ng update " ) && !x.l.Contains( "--migrate-only" ) ).Select( x => x.i ).ToList();
        packageCommands.Count.ShouldBe( 2 );
        for( int s = 0; s < packageCommands.Count; s++ )
        {
            int start = packageCommands[s];
            int firstMigration = lines.FindIndex( start, l => l.Contains( "--migrate-only" ) );
            int dedupe = lines.FindIndex( start, l => l == "yarn dedupe" );
            dedupe.ShouldBeGreaterThan( start );
            int loose = lines.FindIndex( dedupe, l => l == "$env:YARN_PNP_MODE = \"loose\"" );
            loose.ShouldBeGreaterThan( dedupe );
            int looseInstall = lines.FindIndex( loose, l => l == "yarn install" );
            looseInstall.ShouldBeGreaterThan( loose );
            firstMigration.ShouldBeGreaterThan( looseInstall );
            int strictAgain = lines.FindIndex( firstMigration, l => l == "Remove-Item Env:YARN_PNP_MODE" );
            strictAgain.ShouldBeGreaterThan( firstMigration );
            if( s + 1 < packageCommands.Count )
            {
                strictAgain.ShouldBeLessThan( packageCommands[s + 1] );
            }
        }
    }

    [Test]
    public void manual_commands_use_the_installed_version_of_a_library_first_updated_by_the_last_step()
    {
        var steps = AngularUpgradePlanner.Plan( 20, 22, _zorroApp20, _declared22 );
        var installed = new Dictionary<string, SVersion> { { "@fortawesome/angular-fontawesome", SVersion.Parse( "3.2.0" ) } };
        var text = AngularUpgradePlanner.RenderManualCommands( steps, installed );
        text.ShouldContain( "ng update @fortawesome/angular-fontawesome --migrate-only --from 3.2.0 --to 5" );
    }

    [Test]
    public void manual_commands_say_which_failures_to_carry_on_after()
    {
        var steps = AngularUpgradePlanner.Plan( 20, 22, _zorroApp20, _declared22 );
        var text = AngularUpgradePlanner.RenderManualCommands( steps, new Dictionary<string, SVersion>() );
        var lines = text.Split( '\n' ).Select( l => l.TrimEnd( '\r' ) ).ToList();

        int packageCommand = lines.FindLastIndex( l => l.StartsWith( "yarn ng update " ) && !l.Contains( "--migrate-only" ) );
        lines[packageCommand - 1].ShouldBe( "# Carry on when package.json shows @angular/core at major 22." );
        lines[packageCommand - 2].ShouldContain( "Cannot find module '@angular-devkit/core'" );

        int firstMigration = lines.FindIndex( packageCommand, l => l.Contains( "--migrate-only" ) );
        lines[firstMigration - 1].ShouldContain( "\"Package does not provide migrations.\": this is harmless." );
    }

    [Test]
    public void manual_commands_skip_bounds_and_replacements_the_app_does_not_have()
    {
        var app = D( ("@angular/core", "^20.3.0"), ("@angular/cli", "^20.3.0") );
        var steps = AngularUpgradePlanner.Plan( 20, 22, app, _declared22 );
        var text = AngularUpgradePlanner.RenderManualCommands( steps, new Dictionary<string, SVersion>() );
        text.ShouldNotContain( "zone.js" );
        text.ShouldNotContain( "yarn remove" );
    }
}
