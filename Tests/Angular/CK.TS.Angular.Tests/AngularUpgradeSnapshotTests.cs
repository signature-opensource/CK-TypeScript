using CK.Core;
using CK.TS.Angular.Engine;
using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.Angular.Tests;

[TestFixture]
public class AngularUpgradeSnapshotTests
{
    [Test]
    public void prepared_snapshot_is_a_clean_repository_even_when_prepared_again()
    {
        var first = AngularUpgradeSnapshot.Prepare();
        File.WriteAllText( first.AppendPart( "stray.ts" ), "export {}" );

        // The second call deletes the first copy, whose git object files are read-only.
        var path = AngularUpgradeSnapshot.Prepare();

        path.ShouldBe( first );
        var root = AngularGitHelper.Run( path, "rev-parse --show-toplevel", AngularGitHelper.GitTimeout );
        new NormalizedPath( root.Output.FirstOrDefault() ?? "" ).ShouldBe( path );
        File.Exists( path.AppendPart( "stray.ts" ) ).ShouldBeFalse();
        var dirty = AngularGitHelper.GetDirtyFiles( TestHelper.Monitor, path );
        dirty.ShouldNotBeNull();
        dirty.ShouldBeEmpty();
    }
}
