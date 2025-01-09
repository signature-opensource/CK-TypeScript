using CK.Core;
using CK.Transform.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests;

// Use the TranformFunction on "target" to test the string...
// Waiting for my brain to decide what a (PEG) Matcher is.
[TestFixture]
public class RawStringTests
{
    // NUnit TestCase fails with these strings. Using TestCaseSource instead.
    public static object[] Source_valid_single_line_RawString_tests =
    [
        new object[]
        {
            """ /*Empty string*/ "" """,
            ""
        },
        new object[]
        {
            """ /*regular string*/ "I'm a regular string..." """,
            "I'm a regular string..."
        },
        new object[]
        {
            " /*String can end with a quote...*/ \"\"\"sticky \"end\"\"\"\"",
            "sticky \"end\""
        },
        new object[]
        {
            """ /*No backslash escape!*/ "This \n works (with the backslash)" """,
            "This \\n works (with the backslash)"
        }
    ];
    [TestCaseSource( nameof( Source_valid_single_line_RawString_tests ) )]
    public void valid_single_line_RawString_tests( string target, string expected )
    {
        var h = new TransformerHost();
        var f = h.TryParseFunction( TestHelper.Monitor, $"create transform transformer on {target} begin end " );
        Throw.DebugAssert( f != null && f.Target != null );
        f.Target.Should().BeEquivalentTo( expected );
    }

    [TestCase( """
                    /*Invalid end of line!*/

                    "The closing quote is not on this line!
                    "

               """, "Parsing error: Single-line string must not contain end of line.*" )]
    public void invalid_single_line_RawString_tests( string target, string errorMessage )
    {
        var h = new TransformerHost();
        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            h.TryParseFunction( TestHelper.Monitor, $"create transform transformer on {target} begin end" )
                .Should().BeNull();

            logs.Should().ContainMatch( errorMessage );
        }
    }


    [TestCase( """"
                    /*Single empty line*/

                    """

                    """

               """", """

                     """ )]
    [TestCase( """"
                    /*Lines...*/

                            """
                    First,
                    Second,

                    AfterBlank,
                      Below1
                        Below2

                    """

               """", """
                     First,
                     Second,
                     
                     AfterBlank,
                       Below1
                         Below2
                     
                     """ )]
    [TestCase( """"""""""
                    /*As many quotes as needed by the content.*/

                            """"""
                    "",
                    "One",
                    ""Two"",
                    """Three""",
                    """"Four"""",
                    """""Five"""""
                    """"""

               """""""""", """""""
                           "",
                           "One",
                           ""Two"",
                           """Three""",
                           """"Four"""",
                           """""Five"""""
                           """"""" )]
    public void valid_multi_line_RawString_tests( string code, string expected )
    {
        var h = new TransformerHostOld();
        var f = h.ParseFunction( $"create transform transformer on {code} begin end " );
        var t = f.Target as RawStringOld;
        Throw.DebugAssert( t != null );
        var expectedLines = expected.Split( Environment.NewLine );
        t.Lines.Should().BeEquivalentTo( expectedLines );
    }

    [TestCase( """"
                    /*There must be at least one line.*/

                    """
                    """

               """", "Invalid multi-line raw string: at least one line must appear between the \"\"\".*" )]
    [TestCase( """"
                    /*No trailing chars on the first line.*/

                    """ NOWAY

                    """

               """", "Invalid multi-line raw string: there must be no character after the opening \"\"\" characters.*" )]
    [TestCase( """"""""
                    /*No leading chars on the last line (error messages display the right number of quotes).*/
               
                    """"""

                  X """"""

               """""""", "Invalid multi-line raw string: there must be no character on the line before the closing \"\"\"\"\"\" characters.*" )]
    [TestCase( """"""""
                    /*No  chars before the ending column (1/2).*/
               
                    """"""
                   X
                    """"""

               """""""", "Invalid multi-line raw string: there must be no character before column 5.*" )]
    [TestCase( """"""""
                    /*No  chars before the ending column (2/2).*/
               
                    """"""
                   XSome
                    """"""

               """""""", "Invalid multi-line raw string: there must be no character before column 5 in '    XSome'.*" )]
    public void invalid_multi_line_RawString_tests( string code, string errorMessage )
    {
        var h = new TransformerHostOld();
        FluentActions.Invoking( () => h.ParseFunction( $"create transform transformer on {code} begin end " ) )
            .Should().Throw<Exception>().WithMessage( errorMessage );
    }


}
