using CK.Core;
using FluentAssertions;
using Namespace.Does.Not.Matter;
using NUnit.Framework;
using System.IO;
using static CK.Testing.MonitorTestHelper;

namespace CK.EmbeddedResources.Tests;

[TestFixture]
public class EmbeddedResourceTypeAttributeTests
{
    [Test]
    public void SomeType_has_a_Res_folder()
    {
        var resources = typeof( AssemblyResourcesTests ).Assembly.GetResources();

        var c = resources.CreateResourcesContainerForType( TestHelper.Monitor, typeof( SomeType ) );
        c.IsValid.Should().BeTrue();

        // Reading content with the File Provider IFileInfo.
        var fileProvider = c.GetFileProvider();
        var data = fileProvider.GetFileInfo( "data.json" );
        using( var s = data.CreateReadStream() )
        using( var r = new StreamReader( s ) )
        {
            r.ReadToEnd().Trim().Should().Be( """{"Hello":"World"}""" );
        }

        // Reading content from a ResourceLocator.
        c.TryGetResource( "data.json", out ResourceLocator resLocator ).Should().BeTrue();
        c.TryGetResource( TestHelper.Monitor, "data.json", out var resLocator2 ).Should().BeTrue();
        var resLocator3 = c.GetResourceLocator( data );
        resLocator.Should().Be( resLocator2 ).And.Be( resLocator3 );

        using( var s = resLocator.GetStream() )
        using( var r = new StreamReader( s ) )
        {
            r.ReadToEnd().Trim().Should().Be( """{"Hello":"World"}""" );
        }

    }
}
