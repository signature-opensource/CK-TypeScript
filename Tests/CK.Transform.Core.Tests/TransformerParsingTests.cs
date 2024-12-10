using CK.Core;
using CK.Transform.TransformLanguage;
using FluentAssertions;
using NUnit.Framework;

namespace CK.Transform.Core.Tests;

[TestFixture]
public class TransformerParsingTests
{
    [Test]
    public void Empty_minimal_function()
    {
        var h = new TransformerHost();
        var f = h.ParseFunction( "create transform transformer as begin end" );
        f.Statements.Should().HaveCount( 0 );
    }

    [Test]
    public void named_function()
    {
        var h = new TransformerHost();
        var f = h.ParseFunction( "create transform transformer MyTransformer as begin end" );
        f.Name.Should().Be( "MyTransformer" );
        f.Target.Should().BeNull();
    }

    [Test]
    public void named_with_empty_target_function()
    {
        var h = new TransformerHost();
        var f = h.ParseFunction( """create transform transformer MyTransformer on "" as begin end""" );
        f.Name.Should().Be( "MyTransformer" );
        var t = f.Target as RawString;
        Throw.DebugAssert( t != null );
        t.MemoryLines.Should().HaveCount( 1 );
        t.Lines[0].Should().Be( "" );
    }

    [Test]
    public void with_target_function()
    {
        var h = new TransformerHost();
        var f = h.ParseFunction( """create transform transformer on "the target!" as begin end""" );
        f.Name.Should().Be( "" );
        var t = f.Target as RawString;
        Throw.DebugAssert( t != null );
        t.Lines.Should().HaveCount( 1 );
        t.Lines[0].Should().Be( "the target!" );
    }

    [Test]
    public void with_multi_but_singleline_text_target_function()
    {
        var h = new TransformerHost();
        var f = h.ParseFunction( """""
                    create transform transformer on """
                                         create transform transformer as begin end
                                         """
                    begin
                    end
                    """"" );
        f.Name.Should().Be( "" );
        var t = f.Target as RawString;
        Throw.DebugAssert( t != null );
        t.Lines.Should().HaveCount( 1 );
        t.Lines[0].Should().Be( "create transform transformer as begin end" );
    }

}
