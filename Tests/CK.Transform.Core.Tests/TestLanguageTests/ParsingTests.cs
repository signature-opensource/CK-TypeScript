using CK.Core;
using NUnit.Framework;
using Shouldly;

namespace CK.Transform.Core.Tests.TestLanguageTests;

[TestFixture]
public class ParsingTests
{
    [TestCase( "BUG", "BUG" )]
    [TestCase( "Works until BUG", "BUG" )]
    [TestCase( "Works until /*C1*/BUG/*C2*/", "BUG/*C2*/" )]
    public void detecting_blocked_parse( string buggyTest, string remainingText )
    {
        var a = new TestAnalyzer( useSourceSpanBraceAndBrackets: false );
        Should.Throw<CKException>( () => a.Parse( buggyTest ) )
              .Message.ShouldBe( $"""
                Unforwarded head on error at:
                {remainingText}
                Recurring error is:
                Expected BUG! (TokenTypeError: GenericError)
                """ );
    }
}
