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
        create <transform> transformer
        begin
            replace "a" with "b";
        end
        """,
        """"
        create <transform> transformer
        begin
            replace """
                    "a" with "b"
                    """
            with """
                 "b" with "a"
                 """;
        end
        """",
        """
        create <transform> transformer
        begin
            replace "b" with "a";
        end
        """
        )]
    [TestCase( "n°2",
        """
        create <transform> transformer
        begin
            replace /* C1 */ "a" with "b" /* C2 */;
            replace // C3
                "a" with "b"; // C4
        end
        """,
        """"
        create <transform> transformer
        begin
            replace all """
                    "a" with "b"
                    """
            with """
                 "b" with "a"
                 """;
        end
        """",
        """
        create <transform> transformer
        begin
            replace /* C1 */ "b" with "a" /* C2 */;
            replace // C3
                "b" with "a"; // C4
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
