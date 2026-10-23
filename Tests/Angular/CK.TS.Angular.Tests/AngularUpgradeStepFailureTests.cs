using CK.Core;
using CK.TS.Angular.Engine;
using NUnit.Framework;
using Shouldly;
using CSemVer;
using System.Collections.Generic;
using System.Linq;
using static CK.TS.Angular.Engine.AngularCodeGeneratorImpl.AngularCodeGen;

namespace CK.TS.Angular.Tests;

[TestFixture]
public class AngularUpgradeStepFailureTests
{
    static readonly NormalizedPath _app = "D:/Dev/MyApp/CK_TS_Angular";

    static AngularUpgradeStep Plan21()
    {
        var app = new Dictionary<string, SVersionBound> { ["@angular/core"] = SVersionBound.NpmTryParse( "^20.3.0" ).Result };
        var steps = AngularUpgradePlanner.Plan( 20, 21, app, new Dictionary<string, SVersionBound>() );
        return steps.Single();
    }

    [Test]
    public void migration_failure_names_the_failed_package_in_the_command()
    {
        var step = Plan21();
        var package = step.Packages.Single( p => p.Name == "@angular/core" );
        var command = step.MigrationCommand( package, "20.3.33" );
        var failure = new UpgradeFailure( UpgradeFailureKind.MigrationFailed, ["The schematic crashed."], [], "control-flow", null );

        var (doing, found) = DescribeStepFailure( _app, step, new StepFailure( command, new( 1, false ), failure ), [] );

        doing.ShouldContain( "step Angular 20 to 21" );
        doing.ShouldContain( "@angular/core" );
        doing.ShouldContain( $"yarn {command}" );
        found.ShouldContain( "control-flow" );
    }

    [Test]
    public void generic_failure_puts_the_command_and_its_exit_code_in_found()
    {
        var step = Plan21();
        var failure = new UpgradeFailure( UpgradeFailureKind.Generic, ["something unknown"], [], null, null );

        var (_, found) = DescribeStepFailure( _app, step, new StepFailure( "dedupe", new( 7, false ), failure ), [] );

        found.ShouldContain( "'yarn dedupe' ended with exit code 7." );
        found.ShouldContain( "something unknown" );
    }

    [Test]
    public void exception_failure_has_no_command_and_holds_the_message()
    {
        var step = Plan21();

        var (doing, found) = DescribeStepFailure( _app, step, new StepFailure( "", default, null, "An exception stopped the step: disk full" ), [] );

        doing.ShouldNotContain( "running" );
        found.TrimEnd().ShouldBe( "An exception stopped the step: disk full" );
    }
}
