using FluentAssertions;
using NUnit.Framework;

namespace CK.Transform.Core.Tests;

public class SourcePositionTests
{
    [TestCase( "X", 1 )]
    [TestCase( " X", 2 )]
    [TestCase( "  X", 3 )]
    [TestCase( """

               X
               """, 1 )]
    [TestCase( """

                X
               """, 2 )]
    [TestCase( """

                 X
               """, 3 )]
    public void GetColumnNumber( string source, int columnNumber )
    {
        int c = SourcePosition.GetColumNumber( source, source.IndexOf( "X" ) );
        c.Should().Be( columnNumber );
    }
}
