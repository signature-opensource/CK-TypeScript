using CK.Transform.Core;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;

namespace CK.Html.Transform.Tests;

public class HtmlElementSpanTests
{
    [TestCase( "<d></d>", 2, 0 )]
    [TestCase( "A<d></d>B", 4, 1 )]
    [TestCase( "A<d></d>", 3, 1 )]
    [TestCase( "<d></d>B", 3, 0 )]
    public void single_empty_element_tests( string text, int tokenCount, int idxStart )
    {
        var a = new HtmlAnalyzer();
        var sourceCode = a.ParseOrThrow( text );
        sourceCode.Tokens.Count.Should().Be( tokenCount );
        sourceCode.Spans.Count().Should().Be( 1 );
        var s = sourceCode.Spans.Single();
        s.Parent.Should().BeNull();
        s.Children.Should().BeEmpty();
        s.Span.Should().Be( new TokenSpan( idxStart, idxStart + 2 ) );
    }

    [TestCase( "<area >", 1 )]
    [TestCase( "A<base       > ", 3 )]
    [TestCase( " <br> ", 3 )]
    [TestCase( "<col  > ", 2 )]
    [TestCase( "<embed    >", 1 )]
    [TestCase( "<hr>", 1 )]
    [TestCase( "<img>", 1 )]
    [TestCase( "<input       >", 1 )]
    [TestCase( "<link>", 1 )]
    [TestCase( "<meta>", 1 )]
    [TestCase( "<source>", 1 )]
    [TestCase( "<track>", 1 )]
    [TestCase( "<wbr> ", 2 )]
    [TestCase( "A<d    />B", 3 )]
    [TestCase( "<d      /> ", 2 )]
    public void void_and_auto_closing_without_attributes_have_no_covering_span( string text, int tokenCount )
    {
        var a = new HtmlAnalyzer();
        var sourceCode = a.ParseOrThrow( text );
        sourceCode.Tokens.Count.Should().Be( tokenCount );
        sourceCode.Spans.Should().BeEmpty();
    }

    [TestCase( "<area a > text ", 3 + 1 )]
    [TestCase( "<base X >", 3 )]
    [TestCase( "<br class='hop'> text", 5 + 1 )]
    [TestCase( "<col x >", 3 )]
    [TestCase( "<embed y = ''>", 5 )]
    [TestCase( "<hr some >", 3 )]
    [TestCase( "<img href='http:// this is a string...' >", 5 )]
    [TestCase( "<input x y z='k' >", 7 )]
    [TestCase( "<link m >", 3 )]
    [TestCase( "<meta mm>", 3 )]
    [TestCase( "<source a b c d e f  >", 8 )]
    [TestCase( "<track g = 'ii'>", 5 )]
    [TestCase( "<wbr c=''>", 5 )]
    [TestCase( "<d class='mm' />", 5 )]
    public void void_and_auto_closing_WITH_attributes_have_covering_span( string text, int tokenCount )
    {
        var a = new HtmlAnalyzer();
        var sourceCode = a.ParseOrThrow( text );
        sourceCode.Tokens.Count.Should().Be( tokenCount );
        sourceCode.Spans.Count().Should().Be( 1 );
        var s = sourceCode.Spans.Single();
        s.Parent.Should().BeNull();
        s.Children.Should().BeEmpty();
    }
}
