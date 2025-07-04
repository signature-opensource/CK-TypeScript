using NUnit.Framework;

namespace CK.Transform.Core.Tests.TestLanguageTests;

[TestFixture]
public class TransformTests
{
    [TestCase( "n°1 - {span} only.",
        """
        Quote 1 { They who can give up essential liberty to obtain a little temporary safety deserve neither liberty nor safety. }
        Quote 2 { People demand freedom of speech as a compensation for the freedom of thought which they seldom use. }
        """,
        """"
        create <Test> transformer
        begin
            in all {braces} replace * with "{ erased }";
        end
        """",
        """
        Quote 1 { erased }
        Quote 2 { erased }
        """
        )]
    [TestCase( "n°2 - all {span} where Pattern",
        """
        One A { A not HERE { A HERE} A } A {HERE A}
        """,
        """"
        create <Test> transformer
        begin
            in all {braces} where "HERE" replace all "A" with "YES";
        end
        """",
        """
        One A { A not HERE { YES HERE} A } A {HERE YES}
        """
        )]
    [TestCase( "n°3 - first {span} where Pattern",
        """
        One A { A not HERE { A } A } A { HERE A }
        """,
        """"
        create <Test> transformer
        begin
            in first {braces} where "HERE" replace all "A" with "B";
        end
        """",
        """
        One A { A not HERE { A } A } A { HERE B }
        """
        )]
    [TestCase( "n°4",
        """
        One A { A { A } A } A { A }
        """,
        """"
        create <Test> transformer
        begin
            in first {braces} replace all "A" with "this works";
        end
        """",
        """
        One A { A { this works } A } A { A }
        """
        )]
    [TestCase( "n°5",
        """
        One A { A { A } A } A { A }
        """,
        """"
        create <Test> transformer
        begin
            in first {^braces} 
                in first {braces}
                    replace all "A" with "this works";
        end
        """",
        """
        One A { A { this works } A } A { A }
        """
        )]
    [TestCase( "n°6",
        """
        One A { A { A } A } A { A }
        """,
        """"
        create <Test> transformer
        begin
            in all {braces} replace all "A" with "this works";
        end
        """",
        """
        One A { A { this works } A } A { this works }
        """
        )]
    [TestCase( "n°7",
        """
        One A { A { A } A } A { A }
        """,
        """"
        create <Test> transformer
        begin
            in last {braces} replace all "A" with "this works";
        end
        """",
        """
        One A { A { A } A } A { this works }
        """
        )]
    public void braces_replace( string title, string source, string transformer, string result )
    {
        TestLanguage.StandardTest( source, transformer, result );
    }

    [TestCase( "n°1",
        """
        One A { A { A } A } A { A }
        """,
        """"
        create <Test> transformer
        begin
            in first {^braces} replace all "A" with "this works";
        end
        """",
        """
        One A { this works { this works } this works } A { A }
        """
        )]
    [TestCase( "n°2",
        """
        One A { A { HERE } A } A { A }
        """,
        """"
        create <Test> transformer
        begin
            in first {^braces} where "HERE" replace all "A" with "this works";
        end
        """",
        """
        One A { this works { HERE } this works } A { A }
        """
        )]
    [TestCase( "n°3",
        """
        One A { A { A HERE } A } A { A }
        """,
        """"
        create <Test> transformer
        begin
            in after first "A {"
                in first {^braces} where "HERE" replace all "A" with "this works";
        end
        """",
        """
        One A { A { this works HERE } A } A { A }
        """
        )]
    public void covering_scoped_replace( string title, string source, string transformer, string result )
    {
        TestLanguage.StandardTest( source, transformer, result );
    }

    [TestCase( "n°1",
        """
        someFunc( { import: [] } )
        Module( { x: f( import: [] ), import: [
        ] } )
        class A { import: [] }
        """,
        """"
        create <Test> transformer
        begin
            in after "Module"
                in first {^{}}
                    in after last "import:"
                        replace "[]" with """[ "ITEM" ]""";
        end
        """",
        """
        someFunc( { import: [] } )
        Module( { x: f( import: [] ), import: [ "ITEM" ] } )
        class A { import: [] }
        """
        )]
    public void ts_like_replace( string title, string source, string transformer, string result )
    {
        TestLanguage.StandardTest( source, transformer, result );
    }


    [TestCase( "n°1",
        """
        A
        """,
        """"
        create <Test> transformer
        begin
            replace "A" with "A A";
        end
        """",
        """
        A A
        """
    )]
    [TestCase( "n°2",
        """
        A A A
        """,
        """"
        create <Test> transformer
        begin
            replace all "A" with "A A";
        end
        """",
        """
        A A A A A A
        """
    )]
    public void replace_expand( string title, string source, string transformer, string result )
    {
        TestLanguage.StandardTest( source, transformer, result );
    }

    [TestCase( "n°1",
        """
        A
        """,
        """"
        create <Test> transformer
        begin
            replace "A" with "";
        end
        """",
        ""
    )]
    [TestCase( "n°2",
        """
        A A A
        """,
        """"
        create <Test> transformer
        begin
            replace all "A" with "";
        end
        """",
        "  "
    )]
    public void replace_remove( string title, string source, string transformer, string result )
    {
        TestLanguage.StandardTest( source, transformer, result );
    }


    [TestCase( "n°1",
        """
        A B
        """,
        """"
        create <Test> transformer
        begin
            replace first "A" with "";
            replace first "B" with "";
        end
        """",
        """
         
        """
    )]
    [TestCase( "n°2",
        """
        A B C
        A B C
        """,
        """"
        create <Test> transformer
        begin
            replace first "A" with "";
            replace all 2 "B C" with "X";
        end
        """",
        """
         X
        A X
        """
    )]
    public void replace_multiple( string title, string source, string transformer, string result )
    {
        TestLanguage.StandardTest( source, transformer, result );
    }



}
