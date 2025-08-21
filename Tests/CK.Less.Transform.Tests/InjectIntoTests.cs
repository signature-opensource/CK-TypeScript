using CK.Core;
using CK.Transform.Core;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.Less.Transform.Tests;

public class InjectIntoTests
{
    [TestCase( "n°1",
        """
        .form-group {
           margin-bottom: 15px;
           // <FirstInjectionPointEver/>
        }
        
        """,
        """"
        create <less> transformer
        begin
            inject """

                   witdth: 15px;
                   another-width: 60px;
        
                   """ into <FirstInjectionPointEver>;
        end
        """",
        """
        .form-group {
           margin-bottom: 15px;
           //<FirstInjectionPointEver>
           
           witdth: 15px;
           another-width: 60px;
           
           //</FirstInjectionPointEver>
        }

        """
        )]
    [TestCase( "n°2",
        """
        ck-public-page {
            width: 100%;
            height: 100%;

            //<CKPublicPage />
        }
        """,
        """"
        create <less> transformer
        begin
          inject """
                 background-image: url("/images/background.jpg");
                 background-size: cover;
                 background-position: 70% 50%;
                 display: inline-block;
                 """ into <CKPublicPage>;
        end
        """",
        """
        ck-public-page {
            width: 100%;
            height: 100%;

            //<CKPublicPage >
            background-image: url("/images/background.jpg");
            background-size: cover;
            background-position: 70% 50%;
            display: inline-block;
            //</CKPublicPage>
        }
        """
        )]
    public void first_injection_ever( string title, string source, string transformer, string result )
    {
        var h = new TransformerHost( new LessLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer );
        Throw.DebugAssert( function != null );
        var sourceCode = h.Transform( TestHelper.Monitor, source, function );
        Throw.DebugAssert( sourceCode != null );
        sourceCode.ToString().ShouldBe( result );
    }

}
