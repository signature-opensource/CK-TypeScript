using CK.Core;
using CK.Transform.TransformLanguage;
using FluentAssertions;
using NUnit.Framework;
using System;

namespace CK.Transform.Core.Tests;

// Use the TranformFunction on "target" to test the string...
// Waiting for my brain to decide what a (PEG) Matcher is.
[TestFixture]
public class RawStringTests
{
    [TestCase( """ /*Empty string*/ "" """, "" )]
    [TestCase( """ /*regular string*/ "I'm a regular string..." """, "I'm a regular string..." )]
    [TestCase( " /*String can end with a quote...*/ \"\"\"sticky \"end\"\"\"\"", "sticky \"end\"" )]
    [TestCase( """ /*No backslash escape!*/ "This \n works (with the backslash)" """, "This \\n works (with the backslash)" )]
    public void valid_single_line_RawString_tests( string code, string expected )
    {
        var h = new TransformerHost();
        var f = h.Parse( $"create transform transformer on {code} begin end " );
        var t = f.Target as RawString;
        Throw.DebugAssert( t != null );
        var expectedLines = expected.Split( Environment.NewLine );
        t.Lines.Should().BeEquivalentTo( expectedLines );
    }

    [TestCase( """
                    /*Invalid end of line!*/

                    "The closing quote is not on this line!
                    "

               """, "Single-line string must not contain end of line. (Parameter 'text')" )]
    public void invalid_single_line_RawString_tests( string code, string errorMessage )
    {
        var h = new TransformerHost();
        FluentActions.Invoking( () => h.Parse( $"create transform transformer on {code} begin end " ) )
            .Should().Throw<Exception>().WithMessage( errorMessage );
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
    public void valid_multi_line_RawString_tests( string code, string expected )
    {
        var h = new TransformerHost();
        var f = h.Parse( $"create transform transformer on {code} begin end " );
        var t = f.Target as RawString;
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
        var h = new TransformerHost();
        FluentActions.Invoking( () => h.Parse( $"create transform transformer on {code} begin end " ) )
            .Should().Throw<Exception>().WithMessage( errorMessage );
    }


}
