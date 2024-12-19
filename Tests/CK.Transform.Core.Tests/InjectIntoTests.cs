using CK.Transform.TransformLanguage;
using FluentAssertions;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests;

[TestFixture]
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
        var h = new TransformerHostOld();
        var r = h.Transform( TestHelper.Monitor, source, transformer );
        r.Should().Be( result );
    }

}
