using CK.Core;
using CK.Transform.Core;
using FluentAssertions;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests;

[TestFixture]
public class TransformerParsingTests
{
    [Test]
    public void Empty_minimal_function()
    {
        var h = new TransformerHost();
        var f = h.TryParseFunction( TestHelper.Monitor, "create transform transformer begin end" );
        Throw.DebugAssert( f != null );
        f.Body.Statements.Should().HaveCount( 0 );
    }

    [Test]
    public void named_function()
    {
        var h = new TransformerHost();
        var f = h.TryParseFunction( TestHelper.Monitor, "create transform transformer MyTransformer as begin end" );
        Throw.DebugAssert( f != null );
        f.Name.Should().Be( "MyTransformer" );
        f.Target.Should().BeNull();
    }

    [Test]
    public void named_with_empty_target_function()
    {
        var h = new TransformerHost();
        var f = h.TryParseFunction( TestHelper.Monitor, """create transform transformer MyTransformer on "" as begin end""" );
        Throw.DebugAssert( f != null );
        f.Name.Should().Be( "MyTransformer" );
        f.Target.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void with_target_function()
    {
        var h = new TransformerHost();
        var f = h.TryParseFunction( TestHelper.Monitor, """create transform transformer on "the target!" as begin end""" );
        Throw.DebugAssert( f != null );
        f.Name.Should().BeNull();
        f.Target.Should().Be( "the target!" );
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
        Throw.DebugAssert( f != null );
        f.Name.Should().BeNull();
        f.Target.Should().Be( "Some one-line text." );
    }

    [Test]
    public void target_cannot_be_multiline()
    {
        var h = new TransformerHost();
        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            h.TryParseFunction( TestHelper.Monitor, """""
                    create transform transformer on """
                                         More than
                                         one-line
                                         text.
                                         """
                    begin
                    end
                    """"" )
            .Should().BeNull();
            logs.Should().ContainMatch( "Parsing error: Transformer target must be a single line string.*" );
        }
    }

}
