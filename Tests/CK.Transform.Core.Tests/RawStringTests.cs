using CK.Core;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Immutable;

namespace CK.Transform.Core.Tests;

// Use the TranformFunction on "target" to test the string...
// Waiting for my brain to decide what a (PEG) Matcher is.
[TestFixture]
public class RawStringTests
{
    // NUnit TestCase fails with these strings. Using TestCaseSource instead.
    public static object[] Source_valid_single_line_RawString_tests =
    [
        new []
        {
            """ /*Empty string*/ "" """,
            ""
        },
        new []
        {
            """ /*regular string*/ "I'm a regular string..." """,
            "I'm a regular string..."
        },
        new []
        {
            " /*String can end with a quote...*/ \"\"\"sticky \"end\"\"\"\"",
            "sticky \"end\""
        },
        new []
        {
            """ /*No backslash escape!*/ "This \n works (with the backslash)" """,
            "This \\n works (with the backslash)"
        }
    ];
    [TestCaseSource( nameof( Source_valid_single_line_RawString_tests ) )]
    public void valid_single_line_RawString_tests( string code, string expected )
    {
        var codeSource = new TestAnalyzer().ParseOrThrow( code );
        var rawString = codeSource.Tokens[0] as RawString;
        Throw.DebugAssert( rawString != null );
        rawString.Lines.Length.ShouldBe( 1 );
        rawString.Lines[0].ShouldBe( expected );
    }

    [TestCase( """
                    /*Invalid end of line!*/

                    "The closing quote is not on this line!
                    "
                    ERROR_TOLERANT
               """,
               "Single-line string must not contain end of line." )]
    public void invalid_single_line_RawString_tests( string code, string errorMessage )
    {
        var r = new TestAnalyzer().Parse( code );
        r.Success.ShouldBeFalse();
        Throw.DebugAssert( r.FirstError != null );

        r.SourceCode.Tokens.Count.ShouldBe( 2 );
        r.FirstError.ShouldBeSameAs( r.SourceCode.Tokens[0] );
        r.FirstError.ErrorMessage.ShouldStartWith( errorMessage );

        r.SourceCode.Tokens[1].ToString().ShouldBe( "ERROR_TOLERANT" );
    }


    [TestCase( """Some "no closing quote...  """ )]
    [TestCase( """"
               Some """no closing ""quotes""...
               """" )]
    public void unterminated_string_covers_the_whole_text( string code )
    {
        var r = new TestAnalyzer().Parse( code );
        r.Success.ShouldBeFalse();
        Throw.DebugAssert( r.FirstError != null );
        r.SourceCode.Tokens.Count.ShouldBe( 2 );
        r.FirstError.ShouldBeSameAs( r.SourceCode.Tokens[1] );
        r.FirstError.ErrorMessage.ShouldStartWith( "Unterminated string (quote is \")." );

        r.SourceCode.Tokens[0].ToString().ShouldBe( "Some" );
        r.SourceCode.Tokens[1].Text.Length.ShouldBe( code.Length - 4 - 1 );
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
        var codeSource = new TestAnalyzer().ParseOrThrow( code );
        var rawString = codeSource.Tokens[0] as RawString;
        Throw.DebugAssert( rawString != null );
        rawString.Lines.ShouldBe( expected.Split( Environment.NewLine ).ToImmutableArray() );
    }

    [TestCase( """"
                    /*There must be at least one line.*/

                    """
                    """
                    ERROR_TOLERANT

               """", "Invalid multi-line raw string: at least one line must appear between the \"\"\"." )]
    [TestCase( """"
                    /*No trailing chars on the first line.*/

                    """ NOWAY

                    """
                    ERROR_TOLERANT

               """", "Invalid multi-line raw string: there must be no character after the opening \"\"\" characters." )]
    [TestCase( """"""""
                    /*No leading chars on the last line (error messages display the right number of quotes).*/
               
                    """"""

                  X """"""
                    ERROR_TOLERANT

               """""""", "Invalid multi-line raw string: there must be no character on the line before the closing \"\"\"\"\"\" characters." )]
    [TestCase( """"""""
                    /*No  chars before the ending column (1/2).*/
               
                    """"""
                   X
                    """"""
                    ERROR_TOLERANT

               """""""", "Invalid multi-line raw string: there must be no character before column 5." )]
    [TestCase( """"""""
                    /*No  chars before the ending column (2/2).*/
               
                    """"""
                   XSome
                    """"""
                    ERROR_TOLERANT

               """""""", "Invalid multi-line raw string: there must be no character before column 5 in '    XSome'." )]
    public void invalid_multi_line_RawString_tests( string code, string errorMessage )
    {
        var r = new TestAnalyzer().Parse( code );
        r.Success.ShouldBeFalse();
        Throw.DebugAssert( r.FirstError != null );

        r.SourceCode.Tokens.Count.ShouldBe( 2 );
        r.FirstError.ShouldBeSameAs( r.SourceCode.Tokens[0] );
        r.FirstError.ErrorMessage.ShouldStartWith( errorMessage );

        r.SourceCode.Tokens[1].ToString().ShouldBe( "ERROR_TOLERANT" );
    }


}
