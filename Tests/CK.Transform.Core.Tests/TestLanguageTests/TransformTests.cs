using CK.Transform.Core.Tests.Helpers;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests.TestLanguageTests;

[TestFixture]
public class TransformTests
{
    [TestCase( "n°1",
        """
        Quote 1 { They who can give up essential liberty to obtain a little temporary safety deserve neither liberty nor safety. }
        Quote 2 { People demand freedom of speech as a compensation for the freedom of thought which they seldom use. }
        """,
        """"
        create Test transformer
        begin
            in all {braces} replace * with "{ erased }";
        end
        """",
        """
        Quote 1 { erased }
        Quote 2 { erased }
        """
        )]
    [TestCase( "n°2",
        """
        One A { A HERE { A } A } A { HERE A }
        """,
        """"
        create Test transformer
        begin
            in all {braces} "HERE" replace all "A" with "B";
        end
        """",
        """
        One A { B HERE { B } B } A { HERE B }
        """
        )]
    [TestCase( "n°3",
        """
        One A { A HERE { A } A } A { HERE A }
        """,
        """"
        create Test transformer
        begin
            in first {braces} "HERE" replace all "A" with "B";
        end
        """",
        """
        One A { B HERE { B } B } A { HERE A }
        """
        )]
    public void scoped_replace( string title, string source, string transformer, string result )
    {
        var h = new TransformerHost( new TestLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
        var sourceCode = h.Transform( TestHelper.Monitor, source, function ).ShouldNotBeNull();
        sourceCode.ToString().ShouldBe( result );
    }
}
