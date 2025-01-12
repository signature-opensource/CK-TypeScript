using CK.Transform.Core.Tests.Helpers;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Linq;

namespace CK.Transform.Core.Tests;

[TestFixture]
public class TokenTests
{
    [Test]
    public void TokenType_class_reservation_throws()
    {
        FluentActions.Invoking( () => TokenTypeExtensions.ReserveTokenClass( 0, "This is the error!" ) )
            .Should().Throw<InvalidOperationException>()
                     .WithMessage( "The class 'This is the error!' cannot use n°0, this number is already reserved by 'Error'." );
    }

    [Test]
    public void TokenType_trivia_contains_the_delimiter_lengths()
    {
        TokenType line1 = TokenTypeExtensions.GetTriviaLineCommentType( 1 );
        line1.IsTriviaWhitespace().Should().BeFalse();
        line1.IsTriviaBlockComment().Should().BeFalse();
        line1.IsTriviaLineComment().Should().BeTrue();
        line1.GetTriviaCommentStartLength().Should().Be( 1 );
        line1.GetTriviaCommentEndLength().Should().Be( 0 );

        var tLine1 = new Trivia( line1, $"# smallest delimiter.{Environment.NewLine}" );
        tLine1.IsWhitespace.Should().BeFalse();
        tLine1.IsBlockComment.Should().BeFalse();
        tLine1.IsLineComment.Should().BeTrue();
        tLine1.CommentStartLength.Should().Be( 1 );
        tLine1.CommentEndLength.Should().Be( 0 );

        TokenType line7 = TokenTypeExtensions.GetTriviaLineCommentType( 7 );
        line7.IsTriviaWhitespace().Should().BeFalse();
        line7.IsTriviaBlockComment().Should().BeFalse();
        line7.IsTriviaLineComment().Should().BeTrue();
        line7.GetTriviaCommentStartLength().Should().Be( 7 );
        line7.GetTriviaCommentEndLength().Should().Be( 0 );

        var tLine7 = new Trivia( line7, $"1234567 biggest delimiter is 7 characters long.{Environment.NewLine}" );
        tLine7.IsWhitespace.Should().BeFalse();
        tLine7.IsBlockComment.Should().BeFalse();
        tLine7.IsLineComment.Should().BeTrue();
        tLine7.CommentStartLength.Should().Be( 7 );
        tLine7.CommentEndLength.Should().Be( 0 );

        TokenType block1 = TokenTypeExtensions.GetTriviaBlockCommentType( 1, 1 );
        block1.IsTriviaWhitespace().Should().BeFalse();
        block1.IsTriviaBlockComment().Should().BeTrue();
        block1.IsTriviaLineComment().Should().BeFalse();
        block1.GetTriviaCommentStartLength().Should().Be( 1 );
        block1.GetTriviaCommentEndLength().Should().Be( 1 );

        var tBlock1 = new Trivia( block1, $"# smallest delimiter. #" );
        tBlock1.IsWhitespace.Should().BeFalse();
        tBlock1.IsBlockComment.Should().BeTrue();
        tBlock1.IsLineComment.Should().BeFalse();
        tBlock1.CommentStartLength.Should().Be( 1 );
        tBlock1.CommentEndLength.Should().Be( 1 );

        TokenType block7 = TokenTypeExtensions.GetTriviaBlockCommentType( 7, 7);
        block7.IsTriviaWhitespace().Should().BeFalse();
        block7.IsTriviaBlockComment().Should().BeTrue();
        block7.IsTriviaLineComment().Should().BeFalse();
        block7.GetTriviaCommentStartLength().Should().Be( 7 );
        block7.GetTriviaCommentEndLength().Should().Be( 7 );

        var tBlock7 = new Trivia( block7, $"1234567 smallest delimiter. 1234567" );
        tBlock7.IsWhitespace.Should().BeFalse();
        tBlock7.IsBlockComment.Should().BeTrue();
        tBlock7.IsLineComment.Should().BeFalse();
        tBlock7.CommentStartLength.Should().Be( 7 );
        tBlock7.CommentEndLength.Should().Be( 7 );
    }

    [Test]
    public void TokenType_trivia_on_Error()
    {
        TokenType line1 = TokenTypeExtensions.GetTriviaLineCommentType( 1 ) | TokenType.ErrorClassBit;
        line1.IsTriviaWhitespace().Should().BeFalse();
        line1.IsTriviaBlockComment().Should().BeFalse();
        line1.IsTriviaLineComment().Should().BeTrue();
        line1.GetTriviaCommentStartLength().Should().Be( 1 );
        line1.GetTriviaCommentEndLength().Should().Be( 0 );

        var tLine1 = new Trivia( line1, "# one char." );
        tLine1.IsWhitespace.Should().BeFalse();
        tLine1.IsBlockComment.Should().BeFalse();
        tLine1.IsLineComment.Should().BeTrue();
        tLine1.CommentStartLength.Should().Be( 1 );
        tLine1.CommentEndLength.Should().Be( 0 );
    }

    [Test]
    public void Detached_token_SourcePosition()
    {
        {
            var detached = new Token( TokenType.GenericAny, "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 1 );
            detached.GetSourcePosition().ToString().Should().Be( "1,1" );
        }
        {
            var detached = new Token( TokenType.GenericAny, Trivia.OneSpace, "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 2 );
            detached.GetSourcePosition().ToString().Should().Be( "1,2" );
        }
        {
            var detached = new Token( TokenType.GenericAny, Trivia.NewLine, "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 1 );
            detached.GetSourcePosition().ToString().Should().Be( "2,1" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], Trivia.OneSpace[0]], "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 2 );
            detached.GetSourcePosition().ToString().Should().Be( "2,2" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], Trivia.OneSpace[0], Trivia.OneSpace[0]], "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 3 );
            detached.GetSourcePosition().ToString().Should().Be( "2,3" );
        }
        var lineComment = new Trivia( TokenTypeExtensions.GetTriviaLineCommentType( 1 ), $"# a line comment always end with {Environment.NewLine}" );
        {
            var detached = new Token( TokenType.GenericAny, [lineComment], "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 1 );
            detached.GetSourcePosition().ToString().Should().Be( "2,1" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [lineComment, Trivia.OneSpace[0]], "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 2 );
            detached.GetSourcePosition().ToString().Should().Be( "2,2" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], lineComment, Trivia.OneSpace[0], Trivia.OneSpace[0]], "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 3 );
            detached.GetSourcePosition().ToString().Should().Be( "3,3" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [lineComment, lineComment, Trivia.OneSpace[0], Trivia.OneSpace[0]], "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 3 );
            detached.GetSourcePosition().ToString().Should().Be( "3,3" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [lineComment, Trivia.OneSpace[0], lineComment], "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 1 );
            detached.GetSourcePosition().ToString().Should().Be( "3,1" );
        }
        var blockComment = new Trivia( TokenTypeExtensions.GetTriviaBlockCommentType( 1, 1 ), "# 11 cols #" );
        {
            var detached = new Token( TokenType.GenericAny, [blockComment], "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 12 );
            detached.GetSourcePosition().ToString().Should().Be( "1,12" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [blockComment, lineComment], "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 1 );
            detached.GetSourcePosition().ToString().Should().Be( "2,1" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [lineComment, blockComment], "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 12 );
            detached.GetSourcePosition().ToString().Should().Be( "2,12" );
        }
        var blockComment2lines = new Trivia( TokenTypeExtensions.GetTriviaBlockCommentType( 1, 1 ), $"# {Environment.NewLine} 9 cols #" );
        {
            var detached = new Token( TokenType.GenericAny, [blockComment2lines], "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 10 );
            detached.GetSourcePosition().ToString().Should().Be( "2,10" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], blockComment2lines], "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 10 );
            detached.GetSourcePosition().ToString().Should().Be( "3,10" );
        }
        {
            var detached = new Token( TokenType.GenericAny, [Trivia.NewLine[0], blockComment2lines, lineComment, blockComment2lines], "token", Trivia.Empty );
            detached.GetColumnNumber().Should().Be( 10 );
            detached.GetSourcePosition().ToString().Should().Be( "5,10" );
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
        var a = new TestAnalyzer().ParseOrThrow( text );
        var token = a.Tokens[0];
        token.GetColumnNumber().Should().Be( column );
        token.GetSourcePosition().Should().Be( new SourcePosition( line, column ) );
    }

}
