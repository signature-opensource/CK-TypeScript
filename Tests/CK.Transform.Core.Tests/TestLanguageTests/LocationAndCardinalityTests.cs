using CK.Core;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.Transform.Core.Tests.TestLanguageTests;

[TestFixture]
public class LocationAndCardinalityTests
{
    [TestCase( "n°1",
        """
        A B C D E
        """,
        """"
        create <Test> transformer
        begin
            in before "B" replace "A" with "X";
        end
        """",
        """
        X B C D E
        """
        )]
    public void before( string title, string source, string transformer, string result )
    {
        TestLanguage.StandardTest( source, transformer, result );
    }

    [TestCase( "n°1",
        """
        A B C D E
        """,
        """"
        create <Test> transformer
        begin
            in after "B" replace "E" with "X";
        end
        """",
        """
        A B C D X
        """
        )]
    public void after( string title, string source, string transformer, string result )
    {
        TestLanguage.StandardTest( source, transformer, result );
    }


    [TestCase( "n°1",
        """
        A B C D E
        """,
        """"
        create <Test> transformer
        begin
            in between "B" and "E" replace "C D" with "replaced between two matches";
        end
        """",
        """
        A B replaced between two matches E
        """
        )]
    [TestCase( "n°1bis - between L1 and L2 is a before L2 after L1",
        """
        A B C D E
        """,
        """"
        create <Test> transformer
        begin
            in before "E"
                in after "B"
                    replace "C D" with "replaced between two matches";
        end
        """",
        """
        A B replaced between two matches E
        """
        )]
    [TestCase( "n°1ter - in after L1 in before L2 also works.",
        """
        A B C D E
        """,
        """"
        create <Test> transformer
        begin
            in after "B"
                in before "E"
                    replace "C D" with "replaced between two matches";
        end
        """",
        """
        A B replaced between two matches E
        """
        )]
    public void between( string title, string source, string transformer, string result )
    {
        TestLanguage.StandardTest( source, transformer, result );
    }

    [TestCase( "n°1",
        """
        A B C D E
        """,
        """"
        create <Test> transformer
        begin
            replace single "Z" with "";
        end
        """",
        """
        Expected a single match but got 0.
        Current filter: (all) |> [Pattern] "Z" > [Cardinality] single
        """
    )]
    public void single_failure( string title, string source, string transformer, string error )
    {
        var h = new TransformerHost( new TestLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            h.Transform( TestHelper.Monitor, source, function ).ShouldBeNull();
            logs.ShouldContain( l => l.StartsWith( error ) );
        }
    }

}
