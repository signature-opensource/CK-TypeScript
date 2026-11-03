using CK.TS.Angular.Engine;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;

namespace CK.TS.Angular.Tests;

[TestFixture]
public class ContinuousIntegrationDetectorTests
{
    static string? Detect( params (string Name, string Value)[] variables )
    {
        var environment = new Dictionary<string, string>();
        foreach( var (name, value) in variables )
        {
            environment.Add( name, value );
        }
        return ContinuousIntegrationDetector.Detect( name => environment.GetValueOrDefault( name ) );
    }

    [Test]
    public void no_signal_is_not_ci()
    {
        Detect().ShouldBeNull();
    }

    [Test]
    public void ci_variable_is_detected()
    {
        Detect( ("CI", "true") ).ShouldBe( "CI" );
    }

    [Test]
    public void ci_false_wins_over_every_other_signal()
    {
        Detect( ("CI", "false"), ("BUILD_NUMBER", "12") ).ShouldBeNull();
    }

    [Test]
    public void ci_false_wins_over_a_vendor_signal()
    {
        Detect( ("CI", "false"), ("TF_BUILD", "True") ).ShouldBeNull();
    }

    [Test]
    public void vendor_is_named_before_a_generic_variable_and_the_last_matching_vendor_wins()
    {
        // Jenkins also sets HUDSON_URL, and Hudson comes before Jenkins in the table.
        Detect( ("JENKINS_URL", "http://jenkins.example"), ("BUILD_ID", "7"), ("HUDSON_URL", "http://jenkins.example") ).ShouldBe( "Jenkins" );
    }

    [Test]
    public void vendor_only_signal_is_detected()
    {
        Detect( ("TF_BUILD", "True") ).ShouldBe( "Azure Pipelines" );
    }

    [Test]
    public void vendor_needing_all_variables_needs_them_all()
    {
        Detect( ("JENKINS_URL", "http://jenkins.example") ).ShouldBeNull();
    }
}
