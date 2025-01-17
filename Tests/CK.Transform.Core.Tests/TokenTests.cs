using CK.Core;
using CK.Transform.Core.Tests.Helpers;
using System;
using System.Linq;

namespace CK.Transform.Core.Tests;

public class TokenTests
{
    [Test]
    public void TokenType_class_reservation_throws()
    {
        var msg = Assert.Throws( () => TokenTypeExtensions.ReserveTokenClass( 0, "This is an error!" ) ).Message;
        Throw.CheckState( msg == "The class 'This is an error!' cannot use n°0, this number is already reserved by 'Error'." );
    }

    [Test]
    public void TokenType_trivia_contains_the_delimiter_lengths()
    {
        TokenType line1 = TokenTypeExtensions.GetTriviaLineCommentType( 1 );
        Throw.CheckState( line1.IsTriviaWhitespace() is false );
        Throw.CheckState( line1.IsTriviaBlockComment() is false );
        Throw.CheckState( line1.IsTriviaLineComment() );
        Throw.CheckState( line1.GetTriviaCommentStartLength() == 1 );
        Throw.CheckState( line1.GetTriviaCommentEndLength() == 0 );

        var tLine1 = new Trivia( line1, $"# smallest delimiter.{Environment.NewLine}" );
        Throw.CheckState( tLine1.IsWhitespace is false );
        Throw.CheckState( tLine1.IsBlockComment is false );
        Throw.CheckState( tLine1.IsLineComment );
        Throw.CheckState( tLine1.CommentStartLength == 1 );
        Throw.CheckState( tLine1.CommentEndLength == 0 );

        TokenType line4 = TokenTypeExtensions.GetTriviaLineCommentType( 4 );
        Throw.CheckState( line4.IsTriviaWhitespace() is false );
        Throw.CheckState( line4.IsTriviaBlockComment() is false );
        Throw.CheckState( line4.IsTriviaLineComment() );
        Throw.CheckState( line4.GetTriviaCommentStartLength() ==  4 );
        Throw.CheckState( line4.GetTriviaCommentEndLength() ==  0 );

        var tLine4 = new Trivia( line4, $"1234 delimiter.{Environment.NewLine}" );
        Throw.CheckState( tLine4.IsWhitespace is false );
        Throw.CheckState( tLine4.IsBlockComment is false );
        Throw.CheckState( tLine4.IsLineComment );
        Throw.CheckState( tLine4.CommentStartLength ==  4 );
        Throw.CheckState( tLine4.CommentEndLength ==  0 );

        TokenType line15 = TokenTypeExtensions.GetTriviaLineCommentType( 15 );
        Throw.CheckState( line15.IsTriviaWhitespace() is false );
        Throw.CheckState( line15.IsTriviaBlockComment() is false );
        Throw.CheckState( line15.IsTriviaLineComment() );
        Throw.CheckState( line15.GetTriviaCommentStartLength() ==  15 );
        Throw.CheckState( line15.GetTriviaCommentEndLength() ==  0 );

        var tLine15 = new Trivia( line15, $"123456156789ABCDEF biggest delimiter is 15 characters long.{Environment.NewLine}" );
        Throw.CheckState( tLine15.IsWhitespace is false );
        Throw.CheckState( tLine15.IsBlockComment is false );
        Throw.CheckState( tLine15.IsLineComment );
        Throw.CheckState( tLine15.CommentStartLength ==  15 );
        Throw.CheckState( tLine15.CommentEndLength ==  0 );

        TokenType block1 = TokenTypeExtensions.GetTriviaBlockCommentType( 1, 1 );
        Throw.CheckState( block1.IsTriviaWhitespace() is false );
        Throw.CheckState( block1.IsTriviaBlockComment() );
        Throw.CheckState( block1.IsTriviaLineComment() is false );
        Throw.CheckState( block1.GetTriviaCommentStartLength() ==  1 );
        Throw.CheckState( block1.GetTriviaCommentEndLength() ==  1 );

        var tBlock1 = new Trivia( block1, $"# smallest delimiter. #" );
        Throw.CheckState( tBlock1.IsWhitespace is false );
        Throw.CheckState( tBlock1.IsBlockComment );
        Throw.CheckState( tBlock1.IsLineComment is false );
        Throw.CheckState( tBlock1.CommentStartLength ==  1 );
        Throw.CheckState( tBlock1.CommentEndLength ==  1 );

        TokenType block4 = TokenTypeExtensions.GetTriviaBlockCommentType( 4, 4 );
        Throw.CheckState( block4.IsTriviaWhitespace() is false );
        Throw.CheckState( block4.IsTriviaBlockComment() );
        Throw.CheckState( block4.IsTriviaLineComment() is false );
        Throw.CheckState( block4.GetTriviaCommentStartLength() ==  4 );
        Throw.CheckState( block4.GetTriviaCommentEndLength() ==  4 );

        var tBlock4 = new Trivia( block4, $"1234 delimiter. 1234" );
        Throw.CheckState( tBlock4.IsWhitespace is false );
        Throw.CheckState( tBlock4.IsBlockComment );
        Throw.CheckState( tBlock4.IsLineComment is false );
        Throw.CheckState( tBlock4.CommentStartLength ==  4 );
        Throw.CheckState( tBlock4.CommentEndLength ==  4 );

        TokenType block15_7 = TokenTypeExtensions.GetTriviaBlockCommentType( 15, 7 );
        Throw.CheckState( block15_7.IsTriviaWhitespace() is false );
        Throw.CheckState( block15_7.IsTriviaBlockComment() );
        Throw.CheckState( block15_7.IsTriviaLineComment() is false );
        Throw.CheckState( block15_7.GetTriviaCommentStartLength() ==  15 );
        Throw.CheckState( block15_7.GetTriviaCommentEndLength() ==  7 );

        var tblock15_7 = new Trivia( block15_7, $"123456789ABCDEF biggest delimiter. 1234567" );
        Throw.CheckState( tblock15_7.IsWhitespace is false );
        Throw.CheckState( tblock15_7.IsBlockComment );
        Throw.CheckState( tblock15_7.IsLineComment is false );
        Throw.CheckState( tblock15_7.CommentStartLength ==  15 );
        Throw.CheckState( tblock15_7.CommentEndLength ==  7 );

        TokenType blockCDATA = TokenTypeExtensions.GetTriviaBlockCommentType( 9, 3 );
        Throw.CheckState( blockCDATA.IsTriviaWhitespace() is false );
        Throw.CheckState( blockCDATA.IsTriviaBlockComment() );
        Throw.CheckState( blockCDATA.IsTriviaLineComment() is false );
        Throw.CheckState( blockCDATA.GetTriviaCommentStartLength() ==  9 );
        Throw.CheckState( blockCDATA.GetTriviaCommentEndLength() ==  3 );

        var tBlockCDATA = new Trivia( blockCDATA, $"<![CDATA[ Xml CDATA is 9,3 ]]>" );
        Throw.CheckState( tBlockCDATA.IsWhitespace is false );
        Throw.CheckState( tBlockCDATA.IsBlockComment );
        Throw.CheckState( tBlockCDATA.IsLineComment is false );
        Throw.CheckState( tBlockCDATA.CommentStartLength ==  9 );
        Throw.CheckState( tBlockCDATA.CommentEndLength ==  3 );
    }

    [Test]
    public void TokenType_trivia_on_Error()
    {
        TokenType line1 = TokenTypeExtensions.GetTriviaLineCommentType( 1 ) | TokenType.ErrorClassBit;
        Throw.CheckState( line1.IsTriviaWhitespace() is false );
        Throw.CheckState( line1.IsTriviaBlockComment() is false );
        Throw.CheckState( line1.IsTriviaLineComment() );
        Throw.CheckState( line1.GetTriviaCommentStartLength() ==  1 );
        Throw.CheckState( line1.GetTriviaCommentEndLength() ==  0 );

        var tLine1 = new Trivia( line1, "# one char." );
        Throw.CheckState( tLine1.IsWhitespace is false );
        Throw.CheckState( tLine1.IsBlockComment is false );
        Throw.CheckState( tLine1.IsLineComment );
        Throw.CheckState( tLine1.CommentStartLength ==  1 );
        Throw.CheckState( tLine1.CommentEndLength ==  0 );
    }

    [Test]
    public void Detached_token_SourcePosition()
    {
        {
            var detached = new Token( TokenType.GenericAny, "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  1 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "1,1" );
        }
        {
            var detached = new Token( TokenType.GenericAny, Trivia.OneSpace, "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  2 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "1,2" );
        }
        {
            var detached = new Token( TokenType.GenericAny, Trivia.NewLine, "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  1 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "2,1" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], Trivia.OneSpace[0]], "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  2 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "2,2" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], Trivia.OneSpace[0], Trivia.OneSpace[0]], "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  3 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "2,3" );
        }
        var lineComment = new Trivia( TokenTypeExtensions.GetTriviaLineCommentType( 1 ), $"# a line comment always end with {Environment.NewLine}" );
        {
            var detached = new Token( TokenType.GenericAny, [lineComment], "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() == 1 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "2,1" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [lineComment, Trivia.OneSpace[0]], "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  2 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "2,2" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], lineComment, Trivia.OneSpace[0], Trivia.OneSpace[0]], "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  3 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "3,3" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [lineComment, lineComment, Trivia.OneSpace[0], Trivia.OneSpace[0]], "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  3 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "3,3" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [lineComment, Trivia.OneSpace[0], lineComment], "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  1 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "3,1" );
        }
        var blockComment = new Trivia( TokenTypeExtensions.GetTriviaBlockCommentType( 1, 1 ), "# 11 cols #" );
        {
            var detached = new Token( TokenType.GenericAny, [blockComment], "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  12 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "1,12" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [blockComment, lineComment], "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  1 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "2,1" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [lineComment, blockComment], "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  12 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "2,12" );
        }
        var blockComment2lines = new Trivia( TokenTypeExtensions.GetTriviaBlockCommentType( 1, 1 ), $"# {Environment.NewLine} 9 cols #" );
        {
            var detached = new Token( TokenType.GenericAny, [blockComment2lines], "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  10 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "2,10" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], blockComment2lines], "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  10 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "3,10" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], blockComment2lines, lineComment, blockComment2lines], "token", Trivia.Empty );
            Throw.CheckState( detached.GetColumnNumber() ==  10 );
            Throw.CheckState( detached.GetSourcePosition().ToString() ==  "5,10" );
        }
    }

    [Test]
    [Arguments( "n°1", "token", 1, 1 )]
    [Arguments( "n°2", " token", 1, 2 )]
    [Arguments( "n°3", """
               // some comments...
                    token
               """, 2, 6 )]
    [Arguments( "n°4", """
               // some comments...
                /* c */ token
               """, 2, 10 )]
    [Arguments( "n°5", """
               // some comments...
                /* c
                     */ token
               """, 3, 10 )]
    [Arguments( "n°6", """
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
        var a = new TestAnalyzer().ParseOrThrow( text );
        var token = a.Tokens[0];
        Throw.CheckState( token.GetColumnNumber() ==  column );
        Throw.CheckState( token.GetSourcePosition() ==  new SourcePosition( line, column ) );
    }

}
