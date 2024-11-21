using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System.IO;
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

        // FileSystemResourceContainer works with the platform spearator.
        var normalizedPrefix = TestHelper.TestProjectFolder.Path.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar ) + Path.DirectorySeparatorChar;

        c.ResourcePrefix.Should().Be( normalizedPrefix );

        c.GetFolder( "SomeType" ).IsValid.Should().BeTrue();
        c.GetFolder( "C1/Res/" ).IsValid.Should().BeTrue();
        c.GetFolder( "C1/Res" ).IsValid.Should().BeTrue();
        c.GetResource( "AssemblyResourcesTests.cs" ).IsValid.Should().BeTrue();

        c.TryGetResource( "SomeType/SomeType.cs", out var locator ).Should().BeTrue();

        locator.ResourceName.Should().Be( TestHelper.TestProjectFolder.AppendPart( "SomeType" ).AppendPart( "SomeType.cs" )
                                            .Path.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar ) );
        var content = c.GetFolder( "SomeType" );
        content.AllResources.Should().HaveCount( 2 );
        var theOne = content.Resources.Single();
        theOne.ResourceName.Should().EndWith( $"{Path.DirectorySeparatorChar}SomeType.cs" );
    }

    [Test]
    public void FileSystemResourceContainer_AllResources()
    {
        var c = new FileSystemResourceContainer( TestHelper.TestProjectFolder, "This test" );
        c.AllResources.Should().Contain( new ResourceLocator( c, c.ResourcePrefix + "FileSystemResourceContainerTests.cs" ) );
        c.AllResources.Should().Contain( new ResourceLocator( c, c.ResourcePrefix + $"C1{Path.DirectorySeparatorChar}Res{Path.DirectorySeparatorChar}data.json" ) );
    }

}
