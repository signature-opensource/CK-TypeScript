using CK.TS.Angular.Engine;
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;

namespace CK.TS.Angular.Tests;

[TestFixture]
public class AngularUpgradeMarkerTests
{
    string _app = null!;

    [SetUp]
    public void SetUp()
    {
        _app = Path.Combine( Path.GetTempPath(), "ck-angular-marker-" + Guid.NewGuid().ToString( "N" ) );
        Directory.CreateDirectory( _app );
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete( _app, recursive: true );
    }

    [Test]
    public void written_marker_is_read_back()
    {
        var started = new DateTime( 2026, 10, 23, 9, 41, 7, 512, DateTimeKind.Utc );

        new AngularUpgradeMarker( 20, started ).Write( _app );
        var marker = AngularUpgradeMarker.Read( _app );

        marker.ShouldNotBeNull();
        marker.FromMajor.ShouldBe( 20 );
        marker.StartedUtc.ShouldBe( new DateTime( 2026, 10, 23, 9, 41, 7, DateTimeKind.Utc ) );
        marker.StartedUtc.Kind.ShouldBe( DateTimeKind.Utc );
    }

    [Test]
    public void missing_marker_reads_null_and_does_not_exist()
    {
        AngularUpgradeMarker.Exists( _app ).ShouldBeFalse();
        AngularUpgradeMarker.Read( _app ).ShouldBeNull();
    }

    [Test]
    public void unreadable_marker_exists_but_reads_null()
    {
        File.WriteAllText( Path.Combine( _app, AngularUpgradeMarker.FileName ), "garbage" );

        AngularUpgradeMarker.Exists( _app ).ShouldBeTrue();
        AngularUpgradeMarker.Read( _app ).ShouldBeNull();
    }

    [Test]
    public void delete_removes_the_file()
    {
        var path = Path.Combine( _app, AngularUpgradeMarker.FileName );
        File.WriteAllText( path, "fromMajor=20" );

        AngularUpgradeMarker.Delete( _app );

        File.Exists( path ).ShouldBeFalse();
    }
}
