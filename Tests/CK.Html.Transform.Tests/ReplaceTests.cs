using CK.Transform.Core;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.Html.Transform.Tests;

public class ReplaceTests
{
    // Tokens must match... Exactly!
    // This is barely usable: here "some html..." will fail on "some html... ".
    //
    // We should introduce a "inside token matcher". The problem is to transfer the
    // result of a "sub match" to above statements (like the "replace").
    // Sub matches (that may also applies to matches in trivias) can be costly and
    // should not interfere with the TokenSpan [x,y[ token-level model.
    //
    // Not an easy piece...
    // 
    [TestCase( "n°1",
        """
        some html...
        """,
        """"
        create <html> transformer
        begin
            replace "some html..." with "Some more html!";
        end
        """",
        """
        Some more html!
        """
        )]
    public void replace( string title, string source, string transformer, string result )
    {
        var h = new TransformerHost( new HtmlLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
        var sourceCode = h.Transform( TestHelper.Monitor, source, function ).ShouldNotBeNull();
        sourceCode.ToString().ShouldBe( result );
    }

}
