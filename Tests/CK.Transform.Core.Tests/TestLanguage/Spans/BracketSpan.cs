using CK.Core;

namespace CK.Transform.Core.Tests;

sealed class BracketSpan : SourceSpan
{
    public BracketSpan( int beg, int end )
        : base( beg, end )
    {
    }

    internal static BracketSpan? Match( TestAnalyzer.Scanner scanner, ref TokenizerHead head, Token openBracketToken )
    {
        Throw.DebugAssert( !head.IsCondemned
                           && head.LastToken == openBracketToken
                           && openBracketToken.TokenType == TokenType.OpenBracket
                           && scanner.BracketDepth > 0 );
        int begSpan = head.LastTokenIndex;
        int expectedDepth = scanner.BracketDepth - 1;
        for(; ; )
        {
            var t = scanner.GetNextToken( ref head );
            if( scanner.BracketDepth == expectedDepth )
            {
                return head.FirstError == null
                        ? head.AddSpan( new BracketSpan( begSpan, head.LastTokenIndex + 1 ) )
                        : null;
            }
            if( t.TokenType is TokenType.EndOfInput )
            {
                return null;
            }
            if( t is not TokenError )
            {
                scanner.HandleKnownSpan( ref head, t );
            }
        }
    }
}
