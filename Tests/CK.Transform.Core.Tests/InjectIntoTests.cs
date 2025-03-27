using CK.Core;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests;

public class InjectIntoTests
{
    [TestCase( "One",
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
        end
        """",
        // Handling the leading space here would require a reparse...
        """
        create transform transformer
        begin
        //<FirstInjectionPointEver>
        // First injection ever...
        //</FirstInjectionPointEver>
        end
        """
        )]
    [TestCase( "One+Comment",
        """
        create transform transformer
        begin
        //<FirstInjectionPointEver/>
        // Some comment!
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                   // First injection ever...
                   """ into <FirstInjectionPointEver>;
        end
        """",
        // Handling the leading space here would require a reparse...
        """
        create transform transformer
        begin
        //<FirstInjectionPointEver>
        // First injection ever...
        //</FirstInjectionPointEver>
        // Some comment!
        end
        """
        )]
    [TestCase( "One+2Comment",
        """
        create transform transformer
        begin
        // Above comment!
        //<FirstInjectionPointEver/>
        // Below comment!
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                   // First injection ever...
                   """ into <FirstInjectionPointEver>;
        end
        """",
        // Handling the leading space here would require a reparse...
        """
        create transform transformer
        begin
        // Above comment!
        //<FirstInjectionPointEver>
        // First injection ever...
        //</FirstInjectionPointEver>
        // Below comment!
        end
        """
        )]
    [TestCase( "Pad",
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
        end
        """",
        // Handling the leading space here would require a reparse...
        """
        create transform transformer
        begin
          //<FirstInjectionPointEver>
          // First injection ever...
          //</FirstInjectionPointEver>
        end
        """
        )]
    [TestCase( "Pad+Comment",
        """
        create transform transformer
        begin
          //<FirstInjectionPointEver/>
          // Some comment!
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                   // First injection ever...
                   """ into <FirstInjectionPointEver>;
        end
        """",
        // Handling the leading space here would require a reparse...
        """
        create transform transformer
        begin
          //<FirstInjectionPointEver>
          // First injection ever...
          //</FirstInjectionPointEver>
          // Some comment!
        end
        """
        )]
    [TestCase( "Pad+2Comment",
        """
        create transform transformer
        begin
              // Above comment!
           //<FirstInjectionPointEver/>
              // Below comment!
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                   // First injection ever...
                   """ into <FirstInjectionPointEver>;
        end
        """",
        """
        create transform transformer
        begin
              // Above comment!
           //<FirstInjectionPointEver>
           // First injection ever...
           //</FirstInjectionPointEver>
              // Below comment!
        end
        """
        )]

    #region Already Opened
    [TestCase( "Open",
        """
        create transform transformer
        begin
        //<FirstInjectionPointEver>
        // Already here...
        //</FirstInjectionPointEver>
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                   // ...and another one.
                   """ into <FirstInjectionPointEver>;
        end
        """",
        """
        create transform transformer
        begin
        //<FirstInjectionPointEver>
        // Already here...
        // ...and another one.
        //</FirstInjectionPointEver>
        end
        """
        )]

    [TestCase( "Open+2Comment",
        """
        create transform transformer
        begin
        // Above
        //<FirstInjectionPointEver>
        // Already here...
        //</FirstInjectionPointEver>
        // Below
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                   // ...and another one.
                   """ into <FirstInjectionPointEver>;
        end
        """",
        """
        create transform transformer
        begin
        // Above
        //<FirstInjectionPointEver>
        // Already here...
        // ...and another one.
        //</FirstInjectionPointEver>
        // Below
        end
        """
        )]
    [TestCase( "Open+Pad",
        """
        create transform transformer
        begin
          //<FirstInjectionPointEver>
          // Already here...
          //</FirstInjectionPointEver>
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                   // ...and another one.
                   """ into <FirstInjectionPointEver>;
        end
        """",
        """
        create transform transformer
        begin
          //<FirstInjectionPointEver>
          // Already here...
          // ...and another one.
          //</FirstInjectionPointEver>
        end
        """
        )]
    [TestCase( "Open+Pad+2Comment",
        """
        create transform transformer
        begin
                // Above
          //<FirstInjectionPointEver>
          // Already here...
          //</FirstInjectionPointEver>
                // Below
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                   // ...and another one.
                   """ into <FirstInjectionPointEver>;
        end
        """",
        """
        create transform transformer
        begin
                // Above
          //<FirstInjectionPointEver>
          // Already here...
          // ...and another one.
          //</FirstInjectionPointEver>
                // Below
        end
        """
        )]
    #endregion

    #region Revert Already Opened
    [TestCase( "ROpen",
        """
        create transform transformer
        begin
        //<FirstInjectionPointEver revert>
        // Already here...
        //</FirstInjectionPointEver>
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                   // ...and another one.
                   """ into <FirstInjectionPointEver>;
        end
        """",
        """
        create transform transformer
        begin
        //<FirstInjectionPointEver revert>
        // ...and another one.
        // Already here...
        //</FirstInjectionPointEver>
        end
        """
        )]

    [TestCase( "ROpen+2Comment",
        """
        create transform transformer
        begin
        // Above
        //<FirstInjectionPointEver revert>
        // Already here...
        //</FirstInjectionPointEver>
        // Below
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                   // ...and another one.
                   """ into <FirstInjectionPointEver>;
        end
        """",
        """
        create transform transformer
        begin
        // Above
        //<FirstInjectionPointEver revert>
        // ...and another one.
        // Already here...
        //</FirstInjectionPointEver>
        // Below
        end
        """
        )]
    [TestCase( "ROpen+Pad",
        """
        create transform transformer
        begin
          //<FirstInjectionPointEver revert>
          // Already here...
          //</FirstInjectionPointEver>
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                   // ...and another one.
                   """ into <FirstInjectionPointEver>;
        end
        """",
        """
        create transform transformer
        begin
          //<FirstInjectionPointEver revert>
          // ...and another one.
          // Already here...
          //</FirstInjectionPointEver>
        end
        """
        )]
    [TestCase( "ROpen+Pad+2Comment",
        """
        create transform transformer
        begin
                // Above
          //<FirstInjectionPointEver revert>
          // Already here...
          //</FirstInjectionPointEver>
                // Below
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                   // ...and another one.
                   """ into <FirstInjectionPointEver>;
        end
        """",
        """
        create transform transformer
        begin
                // Above
          //<FirstInjectionPointEver revert>
          // ...and another one.
          // Already here...
          //</FirstInjectionPointEver>
                // Below
        end
        """
        )]
    #endregion

    // When combining 2 (or more) inject without reparse, the
    // game with the trivia fails.
    [TestCase( "Two",
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
        // Handling the leading space here would require a reparse...
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
    [TestCase( "RTwo",
        """
        create transform transformer
        begin
          //<FirstInjectionPointEver revert/>
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
          //<FirstInjectionPointEver revert>
        // ...and another one.
          // First injection ever...
          //</FirstInjectionPointEver>
        end
        """
        )]

    [TestCase( "TwoReparse",
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
            reparse;
            inject """
                   // ...and another one.
                   """ into <FirstInjectionPointEver>;
        end
        """",
        // Handling the leading space here would require a reparse...
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
    [TestCase( "RTwoReparse",
        """
        create transform transformer
        begin
          //<FirstInjectionPointEver revert/>
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                    // First injection ever...
                    """ into <FirstInjectionPointEver>;
            reparse;
            inject """
                   // ...and another one.
                   """ into <FirstInjectionPointEver>;
        end
        """",
        """
        create transform transformer
        begin
          //<FirstInjectionPointEver revert>
          // ...and another one.
          // First injection ever...
          //</FirstInjectionPointEver>
        end
        """
        )]
    public void first_injection_ever( string title, string source, string transformer, string result )
    {
        var h = new TransformerHost();
        var function = h.TryParseFunction( TestHelper.Monitor, transformer );
        Throw.DebugAssert( function != null );
        var sourceCode = h.Transform( TestHelper.Monitor, source, function );
        Throw.DebugAssert( sourceCode != null );
        sourceCode.ToString().ShouldBe( result );
    }

    [TestCase( "PadOne",
        """
        create transform transformer
        begin
          /*<FirstInjectionPointEver/>*/
        end
        """,
        """"
        create transform transformer
        begin
            inject """
                    // Block comment injection is "inline". We need a newline (because this is a line comment!).

                    """ into <FirstInjectionPointEver>;
        end
        """",
        """
        create transform transformer
        begin
          /*<FirstInjectionPointEver>*/// Block comment injection is "inline". We need a newline (because this is a line comment!).
          /*</FirstInjectionPointEver>*/
        end
        """
        )]
    [TestCase( "PadOpened",
        """
        create transform transformer
        begin
          /*<FirstInjectionPointEver>*/ /*exist*/ /*</FirstInjectionPointEver>*/
        end
        """,
        """"
        create transform transformer
        begin
            inject "/*NEW*/   " into <FirstInjectionPointEver>;
        end
        """",
        """
        create transform transformer
        begin
          /*<FirstInjectionPointEver>*/ /*exist*/ /*NEW*/   /*</FirstInjectionPointEver>*/
        end
        """
        )]
    public void injection_with_block_comment( string title, string source, string transformer, string result )
    {
        var h = new TransformerHost();
        var function = h.TryParseFunction( TestHelper.Monitor, transformer );
        Throw.DebugAssert( function != null );
        var sourceCode = h.Transform( TestHelper.Monitor, source, function );
        Throw.DebugAssert( sourceCode != null );
        sourceCode.ToString().ShouldBe( result );
    }
}
