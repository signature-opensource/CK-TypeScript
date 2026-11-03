using CK.Core;
using CK.TS.Angular.Engine;
using CSemVer;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.Angular.Tests;

[TestFixture]
public class YarnLockReaderTests
{
    const string _yarnLock = """
        __metadata:
          version: 8
          cacheKey: 10c0

        "@angular/core-extra@npm:^21.0.0":
          version: 21.1.0
          resolution: "@angular/core-extra@npm:21.1.0"

        "@angular/core@npm:^19.2.0":
          version: 19.2.4
          resolution: "@angular/core@npm:19.2.4"

        "@angular/core@npm:^20, @angular/core@npm:^20.3.0":
          version: 20.3.15
          resolution: "@angular/core@npm:20.3.15"
          dependencies:
            tslib: "npm:^2.3.0"

        axios@npm:^1.7.2:
          version: 1.7.9
          resolution: "axios@npm:1.7.9"

        """;

    [Test]
    public void reads_the_version_of_a_multi_descriptor_entry()
    {
        var aliased = """
            "string-width-cjs@npm:string-width@^4.2.0, string-width@npm:^4.1.0":
              version: 4.2.3
              resolution: "string-width@npm:4.2.3"
            """;
        YarnLockReader.GetInstalledVersion( aliased, "string-width" ).ShouldBe( SVersion.Parse( "4.2.3" ) );
    }

    [Test]
    public void highest_version_wins()
    {
        YarnLockReader.GetInstalledVersion( _yarnLock, "@angular/core" ).ShouldBe( SVersion.Parse( "20.3.15" ) );
    }

    [Test]
    public void missing_package_returns_null()
    {
        YarnLockReader.GetInstalledVersion( _yarnLock, "ng-zorro-antd" ).ShouldBeNull();
    }

    [Test]
    public void prefix_does_not_match_another_package()
    {
        var onlyExtra = """
            "@angular/core-extra@npm:^21.0.0":
              version: 21.1.0
            """;
        YarnLockReader.GetInstalledVersion( onlyExtra, "@angular/core" ).ShouldBeNull();
    }

    [Test]
    public void from_version_falls_back_to_the_major_with_a_warning()
    {
        using( TestHelper.Monitor.CollectEntries( out var entries, LogLevelFilter.Warn ) )
        {
            YarnLockReader.GetFromVersion( TestHelper.Monitor, _yarnLock, "ng-zorro-antd", 20 ).ShouldBe( SVersion.Create( 20, 0, 0 ) );
            entries.Count.ShouldBe( 1 );
            entries[0].Text.ShouldContain( "ng-zorro-antd" );
        }
    }
}
