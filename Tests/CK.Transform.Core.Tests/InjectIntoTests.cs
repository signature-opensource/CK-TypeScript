using CK.Core;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests;

public class InjectIntoTests
{
    [Test]
    [Arguments(
        """
        create transform transformer
        begin
        //<FirstInjectionPointEver/>
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                   // First injection ever...

                   """ into <FirstInjectionPointEver>;
            inject """
                   // ...and another one.

                   """ into <FirstInjectionPointEver>;
        end
        """",
        """
        create transform transformer
        begin
        //<FirstInjectionPointEver>
        // First injection ever...
        // ...and another one.
        //</FirstInjectionPointEver>
        end
        """
        )]
    public async Task first_injection_ever_Async( string source, string transformer, string result )
    {
        var h = new TransformerHost();
        var function = h.TryParseFunction( TestHelper.Monitor, transformer );
        Throw.DebugAssert( function != null );
        var sourceCode = h.Transform( TestHelper.Monitor, source, function );
        Throw.DebugAssert( sourceCode != null );
        await Assert.That( sourceCode.ToString() ).IsEqualTo( result );
    }

}
