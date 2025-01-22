using CK.Transform.Core;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;

namespace CK.Html.Transform.Tests;

public class HtmlElementSpanTests
{
    [Test]
    public void span_tests()
    {
        var a = new HtmlAnalyzer();
        var sourceCode = a.ParseOrThrow( """
            A<d></d>B
            """ );
        sourceCode.Tokens.Count.Should().Be( 4 );
        sourceCode.Spans.Count().Should().Be( 1 );
        var s = sourceCode.Spans.Single();
        s.Parent.Should().BeNull();
        s.Children.Should().BeEmpty();
        s.Span.Should().Be( new TokenSpan( 1, 2 ) );
    }
}
