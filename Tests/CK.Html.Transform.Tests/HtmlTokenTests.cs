using CK.Transform.Core;
using Shouldly;
using NUnit.Framework;

namespace CK.Html.Transform.Tests;

public class HtmlTokenTests
{
    [Test]
    public void empty_parsing()
    {
        var a = new HtmlAnalyzer();
        var sourceCode = a.ParseOrThrow( "" );
        sourceCode.Spans.ShouldBeEmpty();
        sourceCode.Tokens.ShouldBeEmpty();
    }

    [Test]
    public void text_fragment_parsing()
    {
        var a = new HtmlAnalyzer();
        {
            var sourceCode = a.ParseOrThrow( "some text" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "some text" );
        }
        {
            var sourceCode = a.ParseOrThrow( "some text<" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "some text<" );
        }
        {
            var sourceCode = a.ParseOrThrow( "some text<<" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "some text<<" );
        }
        {
            var sourceCode = a.ParseOrThrow( "some text<<<<<" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "some text<<<<<" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "<" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<<<<<" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "<<<<<" );
        }
        {
            var sourceCode = a.ParseOrThrow( ">" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( ">" );
        }
        {
            var sourceCode = a.ParseOrThrow( ">>>>" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( ">>>>" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<a<b < c <  " );
            sourceCode.Tokens.Count.ShouldBe( 3 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "<a" );
            sourceCode.Tokens[1].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[1].ToString().ShouldBe( "<b " );
            sourceCode.Tokens[2].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[2].ToString().ShouldBe( "< c <  " );
        }
        {
            var sourceCode = a.ParseOrThrow( "</" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "</" );
        }
        {
            var sourceCode = a.ParseOrThrow( "</ a" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "</ a" );
        }
    }

    [Test]
    public void unitary_tag_parsing()
    {
        var a = new HtmlAnalyzer();
        {
            var sourceCode = a.ParseOrThrow( "<some>" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlStartingEmptyElement().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "<some>" );
        }
        {
            // Area is a void element.
            var sourceCode = a.ParseOrThrow( "<Area>" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsEmptyVoidElement().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "<Area>" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<SOME  >" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlStartingEmptyElement().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "<SOME  >" );
        }
        {
            // Wbr is a void element.
            var sourceCode = a.ParseOrThrow( "<WBR  >" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsEmptyVoidElement().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "<WBR  >" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<some/>" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlEmptyElement().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "<some/>" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<some />" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlEmptyElement().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "<some />" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<some a = 'd' />" );
            sourceCode.Tokens.Count.ShouldBe( 5 );
            sourceCode.Tokens[0].TokenType.IsHtmlStartingTag().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "<some" );
            sourceCode.Tokens[1].TokenType.ShouldBe( TokenType.GenericIdentifier );
            sourceCode.Tokens[1].ToString().ShouldBe( "a" );
            sourceCode.Tokens[2].TokenType.ShouldBe( TokenType.Equals );
            sourceCode.Tokens[2].ToString().ShouldBe( "=" );
            sourceCode.Tokens[3].TokenType.ShouldBe( TokenType.GenericString );
            sourceCode.Tokens[3].ToString().ShouldBe( "'d'" );
            sourceCode.Tokens[4].TokenType.IsHtmlEndTokenTag().ShouldBeTrue();
            sourceCode.Tokens[4].ToString().ShouldBe( "/>" );
        }
        {
            var sourceCode = a.ParseOrThrow( "<some a = 'd' >" );
            sourceCode.Tokens.Count.ShouldBe( 5 );
            sourceCode.Tokens[0].TokenType.IsHtmlStartingTag().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "<some" );
            sourceCode.Tokens[1].TokenType.ShouldBe( TokenType.GenericIdentifier );
            sourceCode.Tokens[1].ToString().ShouldBe( "a" );
            sourceCode.Tokens[2].TokenType.ShouldBe( TokenType.Equals );
            sourceCode.Tokens[2].ToString().ShouldBe( "=" );
            sourceCode.Tokens[3].TokenType.ShouldBe( TokenType.GenericString );
            sourceCode.Tokens[3].ToString().ShouldBe( "'d'" );
            sourceCode.Tokens[4].TokenType.ShouldBe( TokenType.GreaterThan );
            sourceCode.Tokens[4].ToString().ShouldBe( ">" );
        }
        {
            // Embed is a void element.
            var sourceCode = a.ParseOrThrow( "<embed a = 'd' >" );
            sourceCode.Tokens.Count.ShouldBe( 5 );
            sourceCode.Tokens[0].TokenType.IsHtmlStartingVoidElement().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "<embed" );
            sourceCode.Tokens[1].TokenType.ShouldBe( TokenType.GenericIdentifier );
            sourceCode.Tokens[1].ToString().ShouldBe( "a" );
            sourceCode.Tokens[2].TokenType.ShouldBe( TokenType.Equals );
            sourceCode.Tokens[2].ToString().ShouldBe( "=" );
            sourceCode.Tokens[3].TokenType.ShouldBe( TokenType.GenericString );
            sourceCode.Tokens[3].ToString().ShouldBe( "'d'" );
            sourceCode.Tokens[4].TokenType.ShouldBe( TokenType.GreaterThan );
            sourceCode.Tokens[4].ToString().ShouldBe( ">" );
        }
        {
            // Track is a void element.
            var sourceCode = a.ParseOrThrow( "<track a = value />" );
            sourceCode.Tokens.Count.ShouldBe( 5 );
            sourceCode.Tokens[0].TokenType.IsHtmlStartingVoidElement().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "<track" );
            sourceCode.Tokens[1].TokenType.ShouldBe( TokenType.GenericIdentifier );
            sourceCode.Tokens[1].ToString().ShouldBe( "a" );
            sourceCode.Tokens[2].TokenType.ShouldBe( TokenType.Equals );
            sourceCode.Tokens[2].ToString().ShouldBe( "=" );
            sourceCode.Tokens[3].TokenType.ShouldBe( TokenType.GenericIdentifier );
            sourceCode.Tokens[3].ToString().ShouldBe( "value" );
            sourceCode.Tokens[4].TokenType.IsHtmlEndTokenTag().ShouldBeTrue();
            sourceCode.Tokens[4].ToString().ShouldBe( "/>" );
        }
    }


    [Test]
    public void ending_tag_parsing()
    {
        var a = new HtmlAnalyzer();
        {
            var sourceCode = a.ParseOrThrow( "</div>" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlEndingTag().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "</div>" );
        }
        {
            var sourceCode = a.ParseOrThrow( "</div  >" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlEndingTag().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "</div  >" );
        }
        {
            var sourceCode = a.ParseOrThrow( "</div ignored >" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlEndingTag().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "</div ignored >" );
        }
        {
            var sourceCode = a.ParseOrThrow( "</div ignored / />" );
            sourceCode.Tokens.Count.ShouldBe( 1 );
            sourceCode.Tokens[0].TokenType.IsHtmlEndingTag().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "</div ignored / />" );
        }
        {
            var sourceCode = a.ParseOrThrow( "</div </a </yes>" );
            sourceCode.Tokens.Count.ShouldBe( 3 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "</div " );
            sourceCode.Tokens[1].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[1].ToString().ShouldBe( "</a " );
            sourceCode.Tokens[2].TokenType.IsHtmlEndingTag().ShouldBeTrue();
            sourceCode.Tokens[2].ToString().ShouldBe( "</yes>" );
        }
    }

    [Test]
    public void tag_errors_and_comments()
    {
        var a = new HtmlAnalyzer();
        {
            var sourceCode = a.ParseOrThrow( "<some <!-- comment --> Text " );
            sourceCode.Tokens.Count.ShouldBe( 2 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "<some " );
            sourceCode.Tokens[0].TrailingTrivias.Length.ShouldBe( 1 );
            sourceCode.Tokens[0].TrailingTrivias[0].Content.ToString().ShouldBe( "<!-- comment -->" );

            sourceCode.Tokens[1].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[1].ToString().ShouldBe( " Text " );
            sourceCode.Tokens[1].LeadingTrivias.Length.ShouldBe( 0 );
            sourceCode.Tokens[1].TrailingTrivias.Length.ShouldBe( 0 );
        }
        {
            var sourceCode = a.ParseOrThrow( "</some <!-- comment --> Text " );
            sourceCode.Tokens.Count.ShouldBe( 2 );
            sourceCode.Tokens[0].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[0].ToString().ShouldBe( "</some " );
            sourceCode.Tokens[0].TrailingTrivias.Length.ShouldBe( 1 );
            sourceCode.Tokens[0].TrailingTrivias[0].Content.ToString().ShouldBe( "<!-- comment -->" );

            sourceCode.Tokens[1].TokenType.IsHtmlText().ShouldBeTrue();
            sourceCode.Tokens[1].ToString().ShouldBe( " Text " );
            sourceCode.Tokens[1].LeadingTrivias.Length.ShouldBe( 0 );
            sourceCode.Tokens[1].TrailingTrivias.Length.ShouldBe( 0 );
        }
    }
}
