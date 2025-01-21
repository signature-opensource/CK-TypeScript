using CK.Transform.Core;
using FluentAssertions;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CK.Html.Transform.Tests;

public class HtmlTokenTests
{
    [Test]
    public void empty_parsing()
    {
        var a = new HtmlAnalyzer();
        var sourceCode = a.ParseOrThrow( "" );
        sourceCode.Spans.Should().BeEmpty();
        sourceCode.Tokens.Should().BeEmpty();
    }

    [Test]
    public void text_fragment_parsing()
    {
        var a = new HtmlAnalyzer();
        {
            var sourceCode = a.ParseOrThrow( "some text" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "some text" );
        }
        {
            var sourceCode = a.ParseOrThrow( "some text<" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "some text<" );
        }
        {
            var sourceCode = a.ParseOrThrow( "some text<<" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "some text<<" );
        }
        {
            var sourceCode = a.ParseOrThrow( "some text<<<<<" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "some text<<<<<" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "<" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<<<<<" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "<<<<<" );
        }
        {
            var sourceCode = a.ParseOrThrow( ">" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( ">" );
        }
        {
            var sourceCode = a.ParseOrThrow( ">>>>" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( ">>>>" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<a<b < c <  " );
            sourceCode.Tokens.Should().HaveCount( 3 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "<a" );
            sourceCode.Tokens[1].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[1].ToString().Should().Be( "<b " );
            sourceCode.Tokens[2].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[2].ToString().Should().Be( "< c <  " );
        }
        {
            var sourceCode = a.ParseOrThrow( "</" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "</" );
        }
        {
            var sourceCode = a.ParseOrThrow( "</ a" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "</ a" );
        }
    }

    [Test]
    public void unitary_tag_parsing()
    {
        var a = new HtmlAnalyzer();
        {
            var sourceCode = a.ParseOrThrow( "<some>" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlStartingEmptyElement().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "<some>" );
        }
        {
            // Area is a void element.
            var sourceCode = a.ParseOrThrow( "<Area>" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsEmptyVoidElement().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "<Area>" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<SOME  >" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlStartingEmptyElement().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "<SOME  >" );
        }
        {
            // Wbr is a void element.
            var sourceCode = a.ParseOrThrow( "<WBR  >" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsEmptyVoidElement().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "<WBR  >" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<some/>" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlEmptyElement().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "<some/>" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<some />" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlEmptyElement().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "<some />" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<some a = 'd' />" );
            sourceCode.Tokens.Should().HaveCount( 5 );
            sourceCode.Tokens[0].TokenType.IsHtmlStartingTag().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "<some" );
            sourceCode.Tokens[1].TokenType.Should().Be( TokenType.GenericIdentifier );
            sourceCode.Tokens[1].ToString().Should().Be( "a" );
            sourceCode.Tokens[2].TokenType.Should().Be( TokenType.Equals );
            sourceCode.Tokens[2].ToString().Should().Be( "=" );
            sourceCode.Tokens[3].TokenType.Should().Be( TokenType.GenericString );
            sourceCode.Tokens[3].ToString().Should().Be( "'d'" );
            sourceCode.Tokens[4].TokenType.IsHtmlEndTokenTag().Should().BeTrue();
            sourceCode.Tokens[4].ToString().Should().Be( "/>" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<some a = 'd' >" );
            sourceCode.Tokens.Should().HaveCount( 5 );
            sourceCode.Tokens[0].TokenType.IsHtmlStartingTag().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "<some" );
            sourceCode.Tokens[1].TokenType.Should().Be( TokenType.GenericIdentifier );
            sourceCode.Tokens[1].ToString().Should().Be( "a" );
            sourceCode.Tokens[2].TokenType.Should().Be( TokenType.Equals );
            sourceCode.Tokens[2].ToString().Should().Be( "=" );
            sourceCode.Tokens[3].TokenType.Should().Be( TokenType.GenericString );
            sourceCode.Tokens[3].ToString().Should().Be( "'d'" );
            sourceCode.Tokens[4].TokenType.Should().Be( TokenType.GreaterThan );
            sourceCode.Tokens[4].ToString().Should().Be( ">" );
        }
        {
            // Embed is a void element.
            var sourceCode = a.ParseOrThrow( "<embed a = 'd' >" );
            sourceCode.Tokens.Should().HaveCount( 5 );
            sourceCode.Tokens[0].TokenType.IsHtmlStartingVoidElement().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "<embed" );
            sourceCode.Tokens[1].TokenType.Should().Be( TokenType.GenericIdentifier );
            sourceCode.Tokens[1].ToString().Should().Be( "a" );
            sourceCode.Tokens[2].TokenType.Should().Be( TokenType.Equals );
            sourceCode.Tokens[2].ToString().Should().Be( "=" );
            sourceCode.Tokens[3].TokenType.Should().Be( TokenType.GenericString );
            sourceCode.Tokens[3].ToString().Should().Be( "'d'" );
            sourceCode.Tokens[4].TokenType.Should().Be( TokenType.GreaterThan );
            sourceCode.Tokens[4].ToString().Should().Be( ">" );
        }
        {
            // Track is a void element.
            var sourceCode = a.ParseOrThrow( "<track a = value />" );
            sourceCode.Tokens.Should().HaveCount( 5 );
            sourceCode.Tokens[0].TokenType.IsHtmlStartingVoidElement().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "<track" );
            sourceCode.Tokens[1].TokenType.Should().Be( TokenType.GenericIdentifier );
            sourceCode.Tokens[1].ToString().Should().Be( "a" );
            sourceCode.Tokens[2].TokenType.Should().Be( TokenType.Equals );
            sourceCode.Tokens[2].ToString().Should().Be( "=" );
            sourceCode.Tokens[3].TokenType.Should().Be( TokenType.GenericIdentifier );
            sourceCode.Tokens[3].ToString().Should().Be( "value" );
            sourceCode.Tokens[4].TokenType.IsHtmlEndTokenTag().Should().BeTrue();
            sourceCode.Tokens[4].ToString().Should().Be( "/>" );
        }
    }


    [Test]
    public void ending_tag_parsing()
    {
        var a = new HtmlAnalyzer();
        {
            var sourceCode = a.ParseOrThrow( "</div>" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlEndingTag().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "</div>" );
        }
        {
            var sourceCode = a.ParseOrThrow( "</div  >" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlEndingTag().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "</div  >" );
        }
        {
            var sourceCode = a.ParseOrThrow( "</div ignored >" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlEndingTag().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "</div ignored >" );
        }
        {
            var sourceCode = a.ParseOrThrow( "</div ignored / />" );
            sourceCode.Tokens.Should().HaveCount( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlEndingTag().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "</div ignored / />" );
        }
        {
            var sourceCode = a.ParseOrThrow( "</div </a </yes>" );
            sourceCode.Tokens.Should().HaveCount( 3 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "</div " );
            sourceCode.Tokens[1].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[1].ToString().Should().Be( "</a " );
            sourceCode.Tokens[2].TokenType.IsHtmlEndingTag().Should().BeTrue();
            sourceCode.Tokens[2].ToString().Should().Be( "</yes>" );
        }
    }

    [Test]
    public void tag_errors_and_comments()
    {
        var a = new HtmlAnalyzer();
        {
            var sourceCode = a.ParseOrThrow( "<some <!-- comment --> Text " );
            sourceCode.Tokens.Should().HaveCount( 2 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "<some " );
            sourceCode.Tokens[0].TrailingTrivias.Length.Should().Be( 1 );
            sourceCode.Tokens[0].TrailingTrivias[0].Content.ToString().Should().Be( "<!-- comment -->" );

            sourceCode.Tokens[1].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[1].ToString().Should().Be( " Text " );
            sourceCode.Tokens[1].LeadingTrivias.Length.Should().Be( 0 );
            sourceCode.Tokens[1].TrailingTrivias.Length.Should().Be( 0 );
        }
        {
            var sourceCode = a.ParseOrThrow( "</some <!-- comment --> Text " );
            sourceCode.Tokens.Should().HaveCount( 2 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[0].ToString().Should().Be( "</some " );
            sourceCode.Tokens[0].TrailingTrivias.Length.Should().Be( 1 );
            sourceCode.Tokens[0].TrailingTrivias[0].Content.ToString().Should().Be( "<!-- comment -->" );

            sourceCode.Tokens[1].TokenType.IsHtmlText().Should().BeTrue();
            sourceCode.Tokens[1].ToString().Should().Be( " Text " );
            sourceCode.Tokens[1].LeadingTrivias.Length.Should().Be( 0 );
            sourceCode.Tokens[1].TrailingTrivias.Length.Should().Be( 0 );
        }
    }
}
