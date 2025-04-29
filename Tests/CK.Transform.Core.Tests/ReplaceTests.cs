using CK.Core;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests;

[TestFixture]
public class ReplaceTests
{
    [TestCase( "n°1",
        """
        create transform transformer
        begin
            replace "a" with "b";
        end
        """,
        """"
        create transform transformer
        begin
            replace """
                    "a" with "b""a" with "b"
                    """
            with """
                 "b" with "a"
                 """;
        end
        """",
        """
        create transform transformer
        begin
            replace "b" with "a";
        end
        """
        )]
    public void unscoped_replace( string title, string source, string transformer, string result )
    {
        var h = new TransformerHost();
        var function = h.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
        var sourceCode = h.Transform( TestHelper.Monitor, source, function ).ShouldNotBeNull();
        sourceCode.ToString().ShouldBe( result );
    }

}
