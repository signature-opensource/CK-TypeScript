using CK.Core;
using CK.Transform.Core;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions.Extensions;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests;

public class TransformerParsingTests
{
    [Test]
    public async Task Empty_minimal_function_Async()
    {
        var h = new TransformerHost();
        var f = h.TryParseFunction( TestHelper.Monitor, "create transform transformer begin end" );
        Throw.DebugAssert( f != null );
        await Assert.That( f.Body.Statements.Count ).IsEqualTo( 0 );
    }

    [Test]
    public async Task named_function_Async()
    {
        var h = new TransformerHost();
        var f = h.TryParseFunction( TestHelper.Monitor, "create transform transformer MyTransformer as begin end" );
        Throw.DebugAssert( f != null );
        await Assert.That( f.Name ).IsEqualTo( "MyTransformer" );
        await Assert.That( f.Target ).IsNull();
    }

    [Test]
    public async Task named_with_empty_target_function_Async()
    {
        var h = new TransformerHost();
        var f = h.TryParseFunction( TestHelper.Monitor, """create transform transformer MyTransformer on "" as begin end""" );
        Throw.DebugAssert( f != null );
        await Assert.That( f.Name ).IsEqualTo( "MyTransformer" );
        await Assert.That( f.Target ).IsNotNull().And.HasLength().Zero;
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

}
