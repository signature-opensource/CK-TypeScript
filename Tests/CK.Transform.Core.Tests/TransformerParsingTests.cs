using CK.Core;
using Shouldly;
using NUnit.Framework;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests;

public class TransformerParsingTests
{
    [Test]
    public void Empty_minimal_function()
    {
        var h = new TransformerHost();
        var f = h.TryParseFunction( TestHelper.Monitor, "create transform transformer begin end" );
        Throw.DebugAssert( f != null );
        f.Body.Statements.Count.ShouldBe( 0 );
    }

    [Test]
    public void named_function()
    {
        var h = new TransformerHost();
        var f = h.TryParseFunction( TestHelper.Monitor, "create transform transformer MyTransformer as begin end" );
        Throw.DebugAssert( f != null );
        f.Name.ShouldBe( "MyTransformer" );
        f.Target.ShouldBeNull();
    }

    [Test]
    public void named_with_empty_target_function()
    {
        var h = new TransformerHost();
        var f = h.TryParseFunction( TestHelper.Monitor, """create transform transformer MyTransformer on "" as begin end""" );
        Throw.DebugAssert( f != null );
        f.Name.ShouldBe( "MyTransformer" );
        f.Target.ShouldNotBeNull().Length.ShouldBe( 0 );
    }

    [Test]
    public void with_target_function()
    {
        var h = new TransformerHost();
        var f = h.TryParseFunction( TestHelper.Monitor, """create transform transformer on "the target!" as begin end""" );
        Throw.CheckState( f != null );
        Throw.CheckState( f.Name == null );
        Throw.CheckState( f.Target == "the target!" );
    }

    [Test]
    public void with_multi_but_singleline_text_target_function()
    {
        var h = new TransformerHost();
        var f = h.TryParseFunction( TestHelper.Monitor, """""
                    create transform transformer on """
                                         Some one-line text.
                                         """
                    begin
                    end
                    """"" );
        Throw.CheckState( f != null );
        Throw.CheckState( f.Name == null );
        Throw.CheckState( f.Target == "Some one-line text." );
    }

    [Test]
    public void target_cannot_be_multiline()
    {
        var h = new TransformerHost();
        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            Throw.CheckState( h.TryParseFunction( TestHelper.Monitor, """""
                    create transform transformer on """
                                         More than
                                         one-line
                                         text.
                                         """
                    begin
                    end
                    """"" ) == null );
            Throw.CheckState( logs.Any( l => l.StartsWith( "Expected single line (found 3 lines). @1,33 while parsing:" ) ) );
        }
    }

    [TestCase( "" )]
    [TestCase( "/* comment only */" )]
    [TestCase( "not a transform function: doesn't start with create..." )]
    public void TryParseFunction_function_returns_null_when_no_create( string text )
    {
        var h = new TransformerHost();
        var f = h.TryParseFunction( TestHelper.Monitor, text, out var hasError );
        Throw.CheckState( f == null );
        Throw.CheckState( hasError is false );
    }


    [TestCase( "", 0 )]
    [TestCase( "create transform transformer begin end", 1 )]
    [TestCase( "create transform transformer begin end create transform transformer begin end", 2 )]
    [TestCase( """
               create transform transformer begin end /**/ create transform transformer begin end
               create transform transformer begin end create transform transformer begin end //
               """, 4 )]
    [TestCase( """
               create transform transformer begin end /**/ not a transformer.
               """, -1 )]
    public void TryParseFunctions_test( string text, int count )
    {
        var h = new TransformerHost();
        var f = h.TryParseFunctions( TestHelper.Monitor, text );
        if( count < 0 )
        {
            Throw.CheckState( f == null );
        }
        else
        {
            Throw.CheckState( f != null );
            f.Count.ShouldBe( count );
        }
    }

}
