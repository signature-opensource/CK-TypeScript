using CK.Core;
using CK.Transform.Core;
using Shouldly;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.Html.Transform.Tests;

public class InjectIntoTests
{
    [TestCase(
        """
        some html... 
        <hr>
        <div>
            <!--<FirstInjectionPointEver/>-->
        </div>
        ...text.
        """,
        """"
        create html transformer
        begin
            inject """

                   First injection ever...

                   """ into <FirstInjectionPointEver>;
        end
        """",
        """
        some html... 
        <hr>
        <div>
            <!--<FirstInjectionPointEver>-->
            First injection ever...
            <!--</FirstInjectionPointEver>-->
        </div>
        ...text.
        """
        )]
    public void first_injection_ever( string source, string transformer, string result )
    {
        var h = new TransformerHost( new HtmlLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer );
        Throw.DebugAssert( function != null );
        var sourceCode = h.Transform( TestHelper.Monitor, source, function );
        Throw.DebugAssert( sourceCode != null );
        sourceCode.ToString().ShouldBe( result );
    }

}
