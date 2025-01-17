using CK.Transform.Core;
using FluentAssertions;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CK.TypeScript.Transform.Tests;

public class HtmlParsingTests
{
    [Test]
    public void empty_parsing()
    {
        var a = new TypeScriptAnalyzer();
        var sourceCode = a.ParseOrThrow( "" );
        sourceCode.Spans.Should().BeEmpty();
        sourceCode.Tokens.Should().BeEmpty();
    }

    [Test]
    public void unrecognized_token()
    {
        var a = new TypeScriptAnalyzer();
        var result = a.Parse( "🙃" );
        result.HardError.ErrorMessage.Should().Be( "Unrecognized token." );
    }
}
