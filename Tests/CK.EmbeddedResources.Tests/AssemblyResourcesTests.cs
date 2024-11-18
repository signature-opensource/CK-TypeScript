using CK.Core;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Text;
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
}



[TestFixture]
public class AssemblyResourcesTests
{
    [Test]
    public void standard_EmbeddedResources_can_coexist_with_CKEmbeddedResources()
    {
        var r = typeof( AssemblyResourcesTests ).Assembly.GetResources();
        r.AllResourceNames.All.Length.Should().Be( 2 * 7 + 1 );
        r.AllResourceNames.All.Should()
            .Contain( "CK.EmbeddedResources.Tests.C1.Res.Sql.script.sql" )
            .And.Contain( "CK.EmbeddedResources.Tests.C2.Res.Sql.script.sql" );

        r.CKResourceNames.Length.Should().Be( 2 * 6 + 1 );
        r.CKResourceNames.ToArray().All( a => a.StartsWith( "ck@" ) ).Should().BeTrue();
    }

    [Test]
    public void AssemblyResources_can_CreateFileProvider()
    {
        var r = typeof( AssemblyResourcesTests ).Assembly.GetResources();

        IFileProvider provider = r.CreateFileProvider();
        IDirectoryContents root = provider.GetDirectoryContents( "" );
        var b = new StringBuilder();
        Dump( b, root, 0 );
        b.ToString().Should().Be( """
            C1
              Res
                SomeFolder
                  Other
                    empty-file.ts
                  Res
                    empty-file.ts
                    empty-file2.ts
                  empty-file.ts
                Sql
                  script.sql
                data.json
            C2
              Res
                SomeFolder
                  Other
                    empty-file.ts
                  Res
                    empty-file.ts
                    empty-file2.ts
                  empty-file.ts
                Sql
                  script.sql
                data.json
            SomeType
              Res
                data.json

            """ );


    }

    static void Dump( StringBuilder b, IDirectoryContents d, int depth )
    {
        foreach( IFileInfo item in d )
        {
            b.Append( ' ', depth*2 ).AppendLine( item.Name );
            if( item.IsDirectory )
            {
                Dump( b, (IDirectoryContents)item, depth + 1 );
            }
        }
    }

    [TestCase( true )]
    [TestCase( false )]
    public void any_FileProvider_root_folder_can_be_selected( bool fromProvider )
    {
        var r = typeof( AssemblyResourcesTests ).Assembly.GetResources();

        IFileProvider provider = r.CreateFileProvider( fromProvider ? "C1/Res" : null );
        IDirectoryContents root = provider.GetDirectoryContents( fromProvider ? "" : "C1/Res" );
        var b = new StringBuilder();
        Dump( b, root, 0 );
        b.ToString().Should().Be( """
            SomeFolder
              Other
                empty-file.ts
              Res
                empty-file.ts
                empty-file2.ts
              empty-file.ts
            Sql
              script.sql
            data.json

            """ );

        IFileProvider otherProvider = r.CreateFileProvider( fromProvider ? "C1/Res/SomeFolder" : null );
        IDirectoryContents otherRoot = otherProvider.GetDirectoryContents( fromProvider ? "" : "C1/Res/SomeFolder" );
        var bOther = new StringBuilder();
        Dump( bOther, otherRoot, 0 );
        bOther.ToString().Should().Be( """
            Other
              empty-file.ts
            Res
              empty-file.ts
              empty-file2.ts
            empty-file.ts
            
            """ );
    }
}
