using CK.Core;
using Shouldly;
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

        c.ResourcePrefix.ShouldBe( normalizedPrefix );

        c.GetFolder( "SomeType" ).IsValid.ShouldBeTrue();
        c.GetFolder( "C1/Res/" ).IsValid.ShouldBeTrue();
        c.GetFolder( "C1/Res" ).IsValid.ShouldBeTrue();
        c.GetResource( "AssemblyResourcesTests.cs" ).IsValid.ShouldBeTrue();

        c.TryGetResource( "SomeType/SomeType.cs", out var locator ).ShouldBeTrue();

        locator.ResourceName.ShouldBe( TestHelper.TestProjectFolder.AppendPart( "SomeType" ).AppendPart( "SomeType.cs" )
                                            .Path.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar ) );
        var content = c.GetFolder( "SomeType" );
        content.AllResources.Count().ShouldBe( 2 );
        var theOne = content.Resources.Single();
        theOne.ResourceName.ShouldEndWith( $"{Path.DirectorySeparatorChar}SomeType.cs" );
    }

    [Test]
    public void FileSystemResourceContainer_AllResources()
    {
        var c = new FileSystemResourceContainer( TestHelper.TestProjectFolder, "This test" );
        c.AllResources.ShouldContain( new ResourceLocator( c, c.ResourcePrefix + "FileSystemResourceContainerTests.cs" ) );
        c.AllResources.ShouldContain( new ResourceLocator( c, c.ResourcePrefix + $"C1{Path.DirectorySeparatorChar}Res{Path.DirectorySeparatorChar}data.json" ) );
    }

}
