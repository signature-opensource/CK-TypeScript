using CK.Core;
using CK.Transform.Core;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Html.Transform.Tests;

public class InjectIntoTests
{
    [Test]
    [Arguments(
        """
        some html... 
        <hr>
        <div><!--<FirstInjectionPointEver/>--></div>
        ...text.
        """,
        """"
        create html transformer
        begin
            inject """
                   First injection ever...

                   """ into <FirstInjectionPointEver>;
            inject """
                   ...and another one.

                   """ into <FirstInjectionPointEver>;
        end
        """",
        """
        some html... 
        <hr>
        <div><!--<FirstInjectionPointEver>-->
        First injection ever...
        ...and another one.
        <!--</FirstInjectionPointEver>--></div>
        ...text.
        """
        )]
    public async Task first_injection_ever_Async( string source, string transformer, string result )
    {
        var h = new TransformerHost( new HtmlLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer );
        Throw.DebugAssert( function != null );
        var sourceCode = h.Transform( TestHelper.Monitor, source, function );
        Throw.DebugAssert( sourceCode != null );
        await Assert.That( sourceCode.ToString() ).IsEqualTo( result );
    }

}
