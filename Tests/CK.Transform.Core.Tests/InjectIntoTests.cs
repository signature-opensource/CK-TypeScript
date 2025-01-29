using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests;

public class InjectIntoTests
{
    [TestCase(
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
    public void first_injection_ever( string source, string transformer, string result )
    {
        var h = new TransformerHost();
        var function = h.TryParseFunction( TestHelper.Monitor, transformer );
        Throw.DebugAssert( function != null );
        var sourceCode = h.Transform( TestHelper.Monitor, source, function );
        Throw.DebugAssert( sourceCode != null );
        sourceCode.ToString().Should().Be( result );
    }

}
