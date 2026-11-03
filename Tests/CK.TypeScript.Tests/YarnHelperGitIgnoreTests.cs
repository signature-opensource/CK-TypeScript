using CK.Core;
using CK.Setup;
using NUnit.Framework;
using Shouldly;
using System.IO;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests;

[TestFixture]
public class YarnHelperGitIgnoreTests
{
    [Test]
    public void an_existing_gitignore_without_yarn_rules_gets_the_yarn_section()
    {
        NormalizedPath root = TestHelper.TestProjectFolder.Combine( "Obj/YarnHelperGitIgnore" );
        TestHelper.CleanupFolder( root );
        var gitIgnore = root.AppendPart( ".gitignore" );
        File.WriteAllText( gitIgnore, "bin/" );

        YarnHelper.HandleGitIgnore( TestHelper.Monitor, root );

        var lines = File.ReadAllLines( gitIgnore );
        lines.ShouldContain( "bin/" );
        lines.ShouldContain( ".yarn/*" );
        lines.ShouldContain( ".pnp.*" );
    }
}
