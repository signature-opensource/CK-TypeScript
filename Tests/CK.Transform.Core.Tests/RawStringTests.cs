using CK.Core;
using CK.Transform.Core;
using CK.Transform.Core.Tests.Helpers;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests;

// Use the TranformFunction on "target" to test the string...
public class RawStringTests
{
    [Test]
    [Arguments( """ /*Empty string*/ "" """,
                "" )]
    [Arguments( """ /*regular string*/ "I'm a regular string..." """,
                "I'm a regular string...")]
    [Arguments( " /*String can end with a quote...*/ \"\"\"sticky \"end\"\"\"\"",
                "sticky \"end\"" )]
    [Arguments( """ /*No backslash escape!*/ "This \n works (with the backslash)" """,
                "This \\n works (with the backslash)" )]
    public void valid_single_line_RawString_tests( string code, string expected )
    {
        var codeSource = new TestAnalyzer().ParseOrThrow( code );
        var rawString = codeSource.Tokens[0] as RawString;
        Throw.CheckState( rawString != null );
        Throw.CheckState( rawString.Lines.Length == 1 );
        Throw.CheckState( rawString.Lines[0] == expected );
    }

    [Test]
    public async Task invalid_single_line_RawString_tests_Async()
    {
        string code = """
                    /*Invalid end of line!*/

                    "The closing quote is not on this line!
                    "
                    ERROR_TOLERANT
                    """;
        var r = new TestAnalyzer().Parse( code );
        Throw.CheckState( r.Success is false );
        Throw.CheckState( r.FirstError != null );

        Throw.CheckState( r.SourceCode.Tokens.Count == 2 );
        Throw.CheckState( r.FirstError == r.SourceCode.Tokens[0] );
        await Assert.That( r.FirstError.ErrorMessage ).IsEqualTo( "Single-line string must not contain end of line." );

        await Assert.That( r.SourceCode.Tokens[1].ToString() ).IsEqualTo( "ERROR_TOLERANT" );
    }

    [Test]
    [Arguments( """Some "no closing quote...  """ )]
    [Arguments( """"
                Some """no closing ""quotes""...
                """" )]
    public void unterminated_string_covers_the_whole_text( string code )
    {
        var r = new TestAnalyzer().Parse( code );
        Throw.CheckState( r.Success is false );
        Throw.CheckState( r.FirstError != null );
        Throw.CheckState( r.SourceCode.Tokens.Count == 2 );
        Throw.CheckState( r.FirstError == r.SourceCode.Tokens[1] );
        Throw.CheckState( r.FirstError.ErrorMessage.StartsWith( "Unterminated string." ) );

        Throw.CheckState( r.SourceCode.Tokens[0].ToString() == "Some" );
        Throw.CheckState( r.SourceCode.Tokens[1].Text.Length == code.Length - 4 - 1 );
    }

    [Test]
    [Arguments( """"
                    /*Single empty line*/

                    """

                    """

                """", """

                     """ )]
    [Arguments( """"
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
    [Arguments( """"""""""
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
    public async Task valid_multi_line_RawString_tests_Async( string code, string expected )
    {
        var codeSource = new TestAnalyzer().ParseOrThrow( code );
        var rawString = codeSource.Tokens[0] as RawString;
        Throw.CheckState( rawString != null );
        await Assert.That( rawString.Lines ).IsEquivalentTo( expected.Split( Environment.NewLine ) );
    }

    [Test]
    [Arguments( """"
                    /*There must be at least one line.*/

                    """
                    """
                    ERROR_TOLERANT

                """", "Invalid multi-line raw string: at least one line must appear between the \"\"\"." )]
    [Arguments( """"
                    /*No trailing chars on the first line.*/

                    """ NOWAY

                    """
                    ERROR_TOLERANT

                """", "Invalid multi-line raw string: there must be no character after the opening \"\"\" characters." )]
    [Arguments( """"""""
                    /*No leading chars on the last line (error messages display the right number of quotes).*/
               
                    """"""

                  X """"""
                    ERROR_TOLERANT

                """""""", "Invalid multi-line raw string: there must be no character on the line before the closing \"\"\"\"\"\" characters." )]
    [Arguments( """"""""
                    /*No  chars before the ending column (1/2).*/
               
                    """"""
                   X
                    """"""
                    ERROR_TOLERANT

                 """""""", "Invalid multi-line raw string: there must be no character before column 3." )]
    [Arguments( """"""""
                    /*No  chars before the ending column (2/2).*/
               
                    """"""
                   XSome
                    """"""
                    ERROR_TOLERANT

                """""""", "Invalid multi-line raw string: there must be no character before column 4 in '   XSome'." )]
    public async Task invalid_multi_line_RawString_tests_Async( string code, string errorMessage )
    {
        var r = new TestAnalyzer().Parse( code );
        Throw.CheckState( r.Success is false );
        Throw.CheckState( r.FirstError != null );

        await Assert.That( r.SourceCode.Tokens.Count ).IsEqualTo( 2 );
        Throw.CheckState( r.FirstError == r.SourceCode.Tokens[0] );
        await Assert.That( r.FirstError.ErrorMessage ).StartsWith( errorMessage );

        await Assert.That( r.SourceCode.Tokens[1].ToString() ).IsEqualTo( "ERROR_TOLERANT" );
    }


}
