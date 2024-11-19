using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.EmbeddedResources.Tests;

[TestFixture]
public class FileSystemResourceContainerTests
{
    [Test]
    public void FileSystemResourceContainer_simple_tests()
    {
        var c = new FileSystemResourceContainer( TestHelper.TestProjectFolder, "This test" );

        c.ResourcePrefix.Should().Be( TestHelper.TestProjectFolder + '/', "We work with '/': '\' have been normalized." );

        c.HasDirectory( "SomeType" ).Should().BeTrue();
        c.HasDirectory( "C1/Res/" ).Should().BeTrue();
        c.HasDirectory( "C1\\Res\\" ).Should().BeTrue();
        c.HasDirectory( "AssemblyResourcesTests.cs" ).Should().BeFalse();

        c.TryGetResource( "SomeType/SomeType.cs", out var locator ).Should().BeTrue();
        c.TryGetResource( "SomeType\\SomeType.cs", out var locator2 ).Should().BeTrue();
        locator2.Should().Be( locator );

        locator.ResourceName.Should().Be( TestHelper.TestProjectFolder.AppendPart( "SomeType" ).AppendPart( "SomeType.cs" ) );
        var content = c.GetFileProvider().GetDirectoryContents( "SomeType" );
        content.Should().HaveCount( 2 );
        var theOne = content.Single( f => f.Name == "SomeType.cs" );
        var locator3 = c.GetResourceLocator( theOne );
        locator3.Should().Be( locator );
    }

    [Test]
    public void FileSystemResourceContainer_AllResources_and_GetAllResourceLocatorsFrom()
    {
        var c = new FileSystemResourceContainer( TestHelper.TestProjectFolder, "This test" );
        c.AllResources.Should().Contain( new ResourceLocator( c, c.ResourcePrefix + "FileSystemResourceContainerTests.cs" ) );
        c.AllResources.Should().Contain( new ResourceLocator( c, c.ResourcePrefix + "C1/Res/data.json" ) );

        var c2 = c.GetFileProvider().GetDirectoryContents( "C2/Res" );
        c.GetAllResourceLocatorsFrom( c2 ).Should().Contain( new ResourceLocator( c, c.ResourcePrefix + "C2/Res/SomeFolder/empty-file.ts" ) );
    }

}
