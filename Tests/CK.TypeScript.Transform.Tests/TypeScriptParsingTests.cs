using CK.Core;
using CK.Transform.Core;
using Shouldly;
using NUnit.Framework;

namespace CK.TypeScript.Transform.Tests;

public class TypeScriptParsingTests
{
    [Test]
    public void empty_parsing()
    {
        var a = new TypeScriptAnalyzer();
        var sourceCode = a.ParseOrThrow( "" );
        sourceCode.Spans.ShouldBeEmpty();
        sourceCode.Tokens.ShouldBeEmpty();
    }

    [Test]
    public void unrecognized_token()
    {
        var a = new TypeScriptAnalyzer();
        var result = a.Parse( "🙃" );
        Throw.DebugAssert( result != null && result.FirstError != null );
        result.FirstError.ErrorMessage.ShouldBe( "Unrecognized token." );
    }
}
