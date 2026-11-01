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

    // The moved span can be nested anywhere after the target: the spans between them shift, a span that contains both
    // keeps its tokens, a span that contains only the moved one loses them.
    [TestCase( "X { P } { Q { R } S } T", 2, 0, "X { R } { P } { Q S } T",
               "BraceSpan [1,4[, BraceSpan [4,7[, BraceSpan [7,11[" )]
    [TestCase( "{ A { P } B { Q { R } } C } { E }", 3, 1, "{ A { R } { P } B { Q } C } { E }",
               "BraceSpan [0,14[, BraceSpan [2,5[, BraceSpan [5,8[, BraceSpan [9,12[, BraceSpan [14,17[" )]
    [TestCase( "{ A { P { x } } B { Q { R { y } } } C } { E }", 4, 1, "{ A { R { y } } { P { x } } B { Q } C } { E }",
               "BraceSpan [0,20[, BraceSpan [2,8[, BraceSpan [4,7[, BraceSpan [8,14[, BraceSpan [10,13[, BraceSpan [15,18[, BraceSpan [20,23[" )]
    [TestCase( "{ A { P } } { Q { R } }", 3, 1, "{ A { R } { P } } { Q }",
               "BraceSpan [0,9[, BraceSpan [2,5[, BraceSpan [5,8[, BraceSpan [9,12[" )]
    public void moving_nested_spans( string source, int movedIndex, int targetIndex, string expectedCode, string expectedSpans )
    {
        var a = new TestAnalyzer( useSourceSpanBraceAndBrackets: true );
        SourceCode code = a.ParseOrThrow( source );
        var all = code.Spans.AllSpans.ToList();
        using( var editor = new SourceCodeEditor( TestHelper.Monitor, code ) )
        {
            editor.MoveSpanBefore( all[movedIndex], all[targetIndex] );
        }
        code.ToString().ShouldBe( expectedCode );
        code.Spans.AllSpans.Select( s => s.ToString() ).Concatenate().ShouldBe( expectedSpans );
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
