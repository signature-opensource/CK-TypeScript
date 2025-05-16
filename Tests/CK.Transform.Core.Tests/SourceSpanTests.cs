using CK.Core;
using NUnit.Framework;
using System;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests;

[TestFixture]
public class SourceSpanTests
{
    [Test]
    public void moving_spans_of_braces()
    {
        var a = new TestAnalyzer();
        SourceCode code = a.ParseOrThrow( "A { B } C { D } { E }" );
        code.Spans.Count().ShouldBe( 3 );
        code.Spans.Select( s => s.ToString() ).Concatenate()
            .ShouldBe( "BraceSpan [1,4[, BraceSpan [5,8[, BraceSpan [8,11[" );

        using( var editor = new SourceCodeEditor( TestHelper.Monitor, code ) )
        {
            editor.MoveSpanBefore( code.Spans.ElementAt( 1 ), code.Spans.ElementAt( 0 ) );
        }
        code.Spans.Select( s => s.ToString() ).Concatenate()
            .ShouldBe( "BraceSpan [1,4[, BraceSpan [4,7[, BraceSpan [8,11[" );
        code.ToString().ShouldBe( "A { D } { B } C { E }" );
    }

}
