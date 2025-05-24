using CK.Transform.Core;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.Html.Transform.Tests;

[TestFixture]
public class InjectIntoTests
{
    [TestCase( "n°1",
        """
        some html... 
        <hr>
        <div>
            <!--<FirstInjectionPointEver/>-->
        </div>
        ...text.
        """,
        """"
        create <html> transformer
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
    [TestCase( "n°2",
        """
        <!--<FirstInjectionPointEver>-->
        <!--</FirstInjectionPointEver>-->
        TEXT
        """,
        """"
        create <html> transformer
        begin
            inject """
                   Injection n°1...
        
                   """ into <FirstInjectionPointEver>;
            inject """
                   Injection n°2...
        
                   """ into <FirstInjectionPointEver>;
                end
        """",
        """
        <!--<FirstInjectionPointEver>-->
        Injection n°1...
        Injection n°2...
        <!--</FirstInjectionPointEver>-->
        TEXT
        """
        )]
    [TestCase( "n°3",
        """
        TEXT
        <!--<FirstInjectionPointEver revert />-->
        """,
        """"
        create <html> transformer
        begin
            inject """
                   Injection n°1...
        
                   """ into <FirstInjectionPointEver>;
            inject """
                   Injection n°2...
        
                   """ into <FirstInjectionPointEver>;
                end
        """",
        """
        TEXT
        <!--<FirstInjectionPointEver revert>-->Injection n°2...
        Injection n°1...
        <!--</FirstInjectionPointEver>-->
        """
        )]
    public void first_injection_ever( string title, string source, string transformer, string result )
    {
        var h = new TransformerHost( new HtmlLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
        var sourceCode = h.Transform( TestHelper.Monitor, source, function ).ShouldNotBeNull();
        sourceCode.ToString().ShouldBe( result );
    }
}
