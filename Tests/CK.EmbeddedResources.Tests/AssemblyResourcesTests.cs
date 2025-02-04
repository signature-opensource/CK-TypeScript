using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;
using System.Text;

namespace CK.EmbeddedResources.Tests;

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

        var root = r.CreateCKResourceContainer( "", "On the Root");
        var b = new StringBuilder();
        Dump( b, new ResourceFolder( root, root.ResourcePrefix ), 0 );
        b.ToString().ReplaceLineEndings().Should().Be( """
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

            """.ReplaceLineEndings() );
    }

    static void Dump( StringBuilder b, ResourceFolder d, int depth )
    {
        foreach( var f in d.Folders )
        {
            b.Append( ' ', depth * 2 ).Append( f.Name ).AppendLine();
            Dump( b, f, depth + 1 );
        }
        foreach( var f in d.Resources )
        {
            b.Append( ' ', depth * 2 ).Append( f.Name ).AppendLine();
        }
    }

    [TestCase( true )]
    [TestCase( false )]
    public void any_sub_path_can_be_selected( bool fromProvider )
    {
        var r = typeof( AssemblyResourcesTests ).Assembly.GetResources();

        var container = r.CreateCKResourceContainer( fromProvider ? "C1/Res" : "", "Container" );
        container.TryGetFolder( fromProvider ? "" : "C1/Res", out var root );
        var b = new StringBuilder();
        Dump( b, root, 0 );
        b.ToString().ReplaceLineEndings().Should().Be( """
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

            """.ReplaceLineEndings() );

        var otherContainer = r.CreateCKResourceContainer( fromProvider ? "C1/Res/SomeFolder" : "", "Other container" );
        var otherRoot = otherContainer.GetFolder( fromProvider ? "" : "C1/Res/SomeFolder" );
        var bOther = new StringBuilder();
        Dump( bOther, otherRoot, 0 );
        bOther.ToString().ReplaceLineEndings().Should().Be( """
            Other
              empty-file.ts
            Res
              empty-file.ts
              empty-file2.ts
            empty-file.ts
            
            """.ReplaceLineEndings() );
    }
}
