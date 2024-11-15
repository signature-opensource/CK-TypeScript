using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;

namespace CK.EmbeddedResources.Tests;

[TestFixture]
public class AssemblyResourcesTests
{
    [Test]
    public void resources_named_differently_can_coexist_but_EnableDefaultEmbeddedResourceItems_must_be_true()
    {
        var r = typeof( AssemblyResourcesTests ).Assembly.GetResources();
        r.AllResourceNames.All.Length.Should().Be( 4 );
        r.AllResourceNames.All.Should().Contain( "CK.EmbeddedResources.Tests.Res.Sql.script.sql" );

        r.CKResourceNames.Length.Should().Be( 3 );
        r.CKResourceNames.ToArray().All( a => a.StartsWith( "ck@" ) ).Should().BeTrue();
    }
}
