using CK.Core;
using CK.Transform.Core;
using FluentAssertions;
using NUnit.Framework;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Html.Transform.Tests;

public class InjectIntoTests
{
    [TestCase(
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
        <div><!--<FirstInjectionPointEver>-->First injection ever...
        ...and another one.
        <!--</FirstInjectionPointEver>--></div>
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
        sourceCode.ToString().Should().Be( result );
    }

}
