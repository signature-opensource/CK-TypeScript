using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests.TestLanguageTests;

[TestFixture]
public class UnlessTests
{
    [TestCase( "n°1",
        """
        X
        """,
        """"
        create <Test> transformer
        begin
            unless <HasChanged>
                replace "X" with "X X";
        end
        """",
        """
        // <HasChanged />
        X X
        """
        )]
    [TestCase( "n°2",
        """
        // <HasChanged />
        X
        """,
        """"
        create <Test> transformer
        begin
            unless <HasChanged>
                replace "X" with "X X";
        end
        """",
        """
        // <HasChanged />
        X
        """
        )]
    public void unless_at_work( string title, string source, string transformer, string result )
    {
        TestLanguage.StandardTest( source, transformer, result );
    }

}
