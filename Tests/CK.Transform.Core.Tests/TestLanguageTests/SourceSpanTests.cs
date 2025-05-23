using CK.Core;
using NUnit.Framework;
using Shouldly;
using System;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests.TestLanguageTests;

[TestFixture]
public class SourceSpanTests
{
    [Test]
    public void moving_spans_of_braces()
    {
        var a = new TestAnalyzer( useSourceSpanBraceAndBrackets: true );
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

    [Test]
    public void spans_braces_brackets_parens()
    {
        var a = new TestAnalyzer( useSourceSpanBraceAndBrackets: true );
        SourceCode code = a.ParseOrThrow( """
            someFunc( { import: [] } )
            Module( { x: f( import: [] ), import: [
            ] } )
            class A { import: [] }
            """ );
        code.Spans.AllSpans.Count().ShouldBe( 10 );
        code.Spans.AllSpans.Select( s => s.ToString() ).Concatenate()
            .ShouldBe( "ParenSpan [1,9[, BraceSpan [2,8[, BracketSpan [5,7[, ParenSpan [10,28[, BraceSpan [11,27[, ParenSpan [15,21[, BracketSpan [18,20[, BracketSpan [24,26[, BraceSpan [30,36[, BracketSpan [33,35[" );
    }

    [TestCase( "n°1",
        """
        A { X B C X } D E
        """,
        """"
        create <Test> transformer
        begin
            in all {braces} where "X" replace * with "x";
        end
        """",
        """
        A x D E
        """
        )]
    [TestCase( "n°2",
        """
        A { X B C X } D E
        """,
        """"
        create <Test> transformer
        begin
            in each {braces} where "X" replace * with "x";
        end
        """",
        """
        A x D E
        """
        )]
    public void replacements( string title, string source, string transformer, string result )
    {
        var h = new TransformerHost( new TestLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
        var sourceCode = h.Transform( TestHelper.Monitor, source, function ).ShouldNotBeNull();
        sourceCode.ToString().ShouldBe( result );
    }

}
