using CK.Transform.Core;
using System.Threading.Tasks;

namespace CK.Html.Transform.Tests;

public class HtmlParsingTests
{
    [Test]
    public async Task empty_parsing_Async()
    {
        var a = new HtmlAnalyzer();
        var sourceCode = a.ParseOrThrow( "" );
        await Assert.That( sourceCode.Spans ).IsEmpty();
        await Assert.That( sourceCode.Tokens ).IsEmpty();
    }
}
