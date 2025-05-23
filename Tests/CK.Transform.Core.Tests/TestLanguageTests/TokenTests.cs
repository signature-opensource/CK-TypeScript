using Shouldly;
using NUnit.Framework;
using System;

namespace CK.Transform.Core.Tests.TestLanguageTests;

[TestFixture]
public class TokenTests
{
    [Test]
    public void TokenType_class_reservation_throws()
    {
        Should.Throw<InvalidOperationException>( () => TokenTypeExtensions.ReserveTokenClass( 0, "This is the error!" ) )
              .Message.ShouldBe( "The class 'This is the error!' cannot use n°0, this number is already reserved by 'Error'." );
    }

    [Test]
    public void TokenType_trivia_contains_the_delimiter_lengths()
    {
        TokenType line1 = TokenTypeExtensions.GetTriviaLineCommentType( 1 );
        line1.IsTriviaWhitespace().ShouldBeFalse();
        line1.IsTriviaBlockComment().ShouldBeFalse();
        line1.IsTriviaLineComment().ShouldBeTrue();
        line1.GetTriviaCommentStartLength().ShouldBe( 1 );
        line1.GetTriviaCommentEndLength().ShouldBe( 0 );

        var tLine1 = new Trivia( line1, $"# smallest delimiter.{Environment.NewLine}" );
        tLine1.IsWhitespace.ShouldBeFalse();
        tLine1.IsBlockComment.ShouldBeFalse();
        tLine1.IsLineComment.ShouldBeTrue();
        tLine1.CommentStartLength.ShouldBe( 1 );
        tLine1.CommentEndLength.ShouldBe( 0 );

        TokenType line4 = TokenTypeExtensions.GetTriviaLineCommentType( 4 );
        line4.IsTriviaWhitespace().ShouldBeFalse();
        line4.IsTriviaBlockComment().ShouldBeFalse();
        line4.IsTriviaLineComment().ShouldBeTrue();
        line4.GetTriviaCommentStartLength().ShouldBe( 4 );
        line4.GetTriviaCommentEndLength().ShouldBe( 0 );

        var tLine4 = new Trivia( line4, $"1234 delimiter.{Environment.NewLine}" );
        tLine4.IsWhitespace.ShouldBeFalse();
        tLine4.IsBlockComment.ShouldBeFalse();
        tLine4.IsLineComment.ShouldBeTrue();
        tLine4.CommentStartLength.ShouldBe( 4 );
        tLine4.CommentEndLength.ShouldBe( 0 );

        TokenType line15 = TokenTypeExtensions.GetTriviaLineCommentType( 15 );
        line15.IsTriviaWhitespace().ShouldBeFalse();
        line15.IsTriviaBlockComment().ShouldBeFalse();
        line15.IsTriviaLineComment().ShouldBeTrue();
        line15.GetTriviaCommentStartLength().ShouldBe( 15 );
        line15.GetTriviaCommentEndLength().ShouldBe( 0 );

        var tLine15 = new Trivia( line15, $"123456156789ABCDEF biggest delimiter is 15 characters long.{Environment.NewLine}" );
        tLine15.IsWhitespace.ShouldBeFalse();
        tLine15.IsBlockComment.ShouldBeFalse();
        tLine15.IsLineComment.ShouldBeTrue();
        tLine15.CommentStartLength.ShouldBe( 15 );
        tLine15.CommentEndLength.ShouldBe( 0 );

        TokenType block1 = TokenTypeExtensions.GetTriviaBlockCommentType( 1, 1 );
        block1.IsTriviaWhitespace().ShouldBeFalse();
        block1.IsTriviaBlockComment().ShouldBeTrue();
        block1.IsTriviaLineComment().ShouldBeFalse();
        block1.GetTriviaCommentStartLength().ShouldBe( 1 );
        block1.GetTriviaCommentEndLength().ShouldBe( 1 );

        var tBlock1 = new Trivia( block1, $"# smallest delimiter. #" );
        tBlock1.IsWhitespace.ShouldBeFalse();
        tBlock1.IsBlockComment.ShouldBeTrue();
        tBlock1.IsLineComment.ShouldBeFalse();
        tBlock1.CommentStartLength.ShouldBe( 1 );
        tBlock1.CommentEndLength.ShouldBe( 1 );

        TokenType block4 = TokenTypeExtensions.GetTriviaBlockCommentType( 4, 4 );
        block4.IsTriviaWhitespace().ShouldBeFalse();
        block4.IsTriviaBlockComment().ShouldBeTrue();
        block4.IsTriviaLineComment().ShouldBeFalse();
        block4.GetTriviaCommentStartLength().ShouldBe( 4 );
        block4.GetTriviaCommentEndLength().ShouldBe( 4 );

        var tBlock4 = new Trivia( block4, $"1234 delimiter. 1234" );
        tBlock4.IsWhitespace.ShouldBeFalse();
        tBlock4.IsBlockComment.ShouldBeTrue();
        tBlock4.IsLineComment.ShouldBeFalse();
        tBlock4.CommentStartLength.ShouldBe( 4 );
        tBlock4.CommentEndLength.ShouldBe( 4 );

        TokenType block15_7 = TokenTypeExtensions.GetTriviaBlockCommentType( 15, 7 );
        block15_7.IsTriviaWhitespace().ShouldBeFalse();
        block15_7.IsTriviaBlockComment().ShouldBeTrue();
        block15_7.IsTriviaLineComment().ShouldBeFalse();
        block15_7.GetTriviaCommentStartLength().ShouldBe( 15 );
        block15_7.GetTriviaCommentEndLength().ShouldBe( 7 );

        var tblock15_7 = new Trivia( block15_7, $"123456789ABCDEF biggest delimiter. 1234567" );
        tblock15_7.IsWhitespace.ShouldBeFalse();
        tblock15_7.IsBlockComment.ShouldBeTrue();
        tblock15_7.IsLineComment.ShouldBeFalse();
        tblock15_7.CommentStartLength.ShouldBe( 15 );
        tblock15_7.CommentEndLength.ShouldBe( 7 );

        TokenType blockCDATA = TokenTypeExtensions.GetTriviaBlockCommentType( 9, 3 );
        blockCDATA.IsTriviaWhitespace().ShouldBeFalse();
        blockCDATA.IsTriviaBlockComment().ShouldBeTrue();
        blockCDATA.IsTriviaLineComment().ShouldBeFalse();
        blockCDATA.GetTriviaCommentStartLength().ShouldBe( 9 );
        blockCDATA.GetTriviaCommentEndLength().ShouldBe( 3 );

        var tBlockCDATA = new Trivia( blockCDATA, $"<![CDATA[ Xml CDATA is 9,3 ]]>" );
        tBlockCDATA.IsWhitespace.ShouldBeFalse();
        tBlockCDATA.IsBlockComment.ShouldBeTrue();
        tBlockCDATA.IsLineComment.ShouldBeFalse();
        tBlockCDATA.CommentStartLength.ShouldBe( 9 );
        tBlockCDATA.CommentEndLength.ShouldBe( 3 );
    }

    [Test]
    public void TokenType_trivia_on_Error()
    {
        TokenType line1 = TokenTypeExtensions.GetTriviaLineCommentType( 1 ) | TokenType.ErrorClassBit;
        line1.IsTriviaWhitespace().ShouldBeFalse();
        line1.IsTriviaBlockComment().ShouldBeFalse();
        line1.IsTriviaLineComment().ShouldBeTrue();
        line1.GetTriviaCommentStartLength().ShouldBe( 1 );
        line1.GetTriviaCommentEndLength().ShouldBe( 0 );

        var tLine1 = new Trivia( line1, "# one char." );
        tLine1.IsWhitespace.ShouldBeFalse();
        tLine1.IsBlockComment.ShouldBeFalse();
        tLine1.IsLineComment.ShouldBeTrue();
        tLine1.CommentStartLength.ShouldBe( 1 );
        tLine1.CommentEndLength.ShouldBe( 0 );
    }

    [Test]
    public void Detached_token_SourcePosition()
    {
        {
            var detached = new Token( TokenType.GenericAny, "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 1 );
            detached.GetSourcePosition().ToString().ShouldBe( "1,1" );
        }
        {
            var detached = new Token( TokenType.GenericAny, Trivia.OneSpace, "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 2 );
            detached.GetSourcePosition().ToString().ShouldBe( "1,2" );
        }
        {
            var detached = new Token( TokenType.GenericAny, Trivia.NewLine, "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 1 );
            detached.GetSourcePosition().ToString().ShouldBe( "2,1" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], Trivia.OneSpace[0]], "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 2 );
            detached.GetSourcePosition().ToString().ShouldBe( "2,2" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], Trivia.OneSpace[0], Trivia.OneSpace[0]], "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 3 );
            detached.GetSourcePosition().ToString().ShouldBe( "2,3" );
        }
        var lineComment = new Trivia( TokenTypeExtensions.GetTriviaLineCommentType( 1 ), $"# a line comment always end with {Environment.NewLine}" );
        {
            var detached = new Token( TokenType.GenericAny, [lineComment], "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 1 );
            detached.GetSourcePosition().ToString().ShouldBe( "2,1" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [lineComment, Trivia.OneSpace[0]], "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 2 );
            detached.GetSourcePosition().ToString().ShouldBe( "2,2" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], lineComment, Trivia.OneSpace[0], Trivia.OneSpace[0]], "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 3 );
            detached.GetSourcePosition().ToString().ShouldBe( "3,3" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [lineComment, lineComment, Trivia.OneSpace[0], Trivia.OneSpace[0]], "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 3 );
            detached.GetSourcePosition().ToString().ShouldBe( "3,3" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [lineComment, Trivia.OneSpace[0], lineComment], "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 1 );
            detached.GetSourcePosition().ToString().ShouldBe( "3,1" );
        }
        var blockComment = new Trivia( TokenTypeExtensions.GetTriviaBlockCommentType( 1, 1 ), "# 11 cols #" );
        {
            var detached = new Token( TokenType.GenericAny, [blockComment], "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 12 );
            detached.GetSourcePosition().ToString().ShouldBe( "1,12" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [blockComment, lineComment], "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 1 );
            detached.GetSourcePosition().ToString().ShouldBe( "2,1" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [lineComment, blockComment], "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 12 );
            detached.GetSourcePosition().ToString().ShouldBe( "2,12" );
        }
        var blockComment2lines = new Trivia( TokenTypeExtensions.GetTriviaBlockCommentType( 1, 1 ), $"# {Environment.NewLine} 9 cols #" );
        {
            var detached = new Token( TokenType.GenericAny, [blockComment2lines], "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 10 );
            detached.GetSourcePosition().ToString().ShouldBe( "2,10" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], blockComment2lines], "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 10 );
            detached.GetSourcePosition().ToString().ShouldBe( "3,10" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], blockComment2lines, lineComment, blockComment2lines], "token", Trivia.Empty );
            detached.GetColumnNumber().ShouldBe( 10 );
            detached.GetSourcePosition().ToString().ShouldBe( "5,10" );
        }
    }

    [TestCase( "n°1", "token", 1, 1 )]
    [TestCase( "n°2", " token", 1, 2 )]
    [TestCase( "n°3", """
               // some comments...
                    token
               """, 2, 6 )]
    [TestCase( "n°4", """
               // some comments...
                /* c */ token
               """, 2, 10 )]
    [TestCase( "n°5", """
               // some comments...
                /* c
                     */ token
               """, 3, 10 )]
    [TestCase( "n°6", """
               // some comments...
                /* c
                  /* TestAnalyzer AcceptCLikeRecursiveStarComment
                        /* TestAnalyzer AcceptCLikeRecursiveStarComment
                        */
                  */
                     */ token
               """, 7, 10 )]
    public void Parsed_token_SourcePosition( string nTest, string text, int line, int column )
    {
        var a = new TestAnalyzer( useSourceSpanBraceAndBrackets: true ).ParseOrThrow( text );
        var token = a.Tokens[0];
        token.GetColumnNumber().ShouldBe( column );
        token.GetSourcePosition().ShouldBe( new SourcePosition( line, column ) );
    }

}
