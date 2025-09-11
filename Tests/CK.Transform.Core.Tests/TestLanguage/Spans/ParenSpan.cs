using CK.Core;

namespace CK.Transform.Core.Tests;

sealed class ParenSpan : SourceSpan
{
    public ParenSpan( int beg, int end )
        : base( beg, end )
    {
    }

    internal static ParenSpan? Match( TestAnalyzer.Scanner scanner, ref TokenizerHead head, Token openParenToken )
    {
        Throw.DebugAssert( !head.IsCondemned
                           && head.LastToken == openParenToken
                           && openParenToken.TokenType == TokenType.OpenParen
                           && scanner.ParenDepth > 0 );
        int begSpan = head.LastTokenIndex;
        int expectedDepth = scanner.ParenDepth - 1;
        for(; ; )
        {
            var t = scanner.GetNextToken( ref head );
            if( scanner.ParenDepth == expectedDepth )
            {
                return head.FirstError == null
                        ? head.AddSpan( new ParenSpan( begSpan, head.LastTokenIndex + 1 ) )
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
