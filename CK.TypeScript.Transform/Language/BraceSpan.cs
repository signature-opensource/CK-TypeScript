using CK.Core;
using CK.Transform.Core;

namespace CK.TypeScript.Transform;

public sealed class BraceSpan : SourceSpan
{
    BraceSpan( int beg, int end )
        : base( beg, end )
    {
    }

    internal static BraceSpan? Match( TypeScriptAnalyzer analyzer, ref TokenizerHead head, Token openBraceToken )
    {
        Throw.DebugAssert( !head.IsCondemned
                           && head.LastToken == openBraceToken
                           && openBraceToken.TokenType == TokenType.OpenBrace
                           && analyzer.BraceDepth > 0 );
        int begSpan = head.LastTokenIndex;
        int expectedDepth = analyzer.BraceDepth - 1;
        for( ; ; )
        {
            var t = analyzer.GetNextToken( ref head );
            if( analyzer.BraceDepth == expectedDepth )
            {
                return head.FirstParseError != null
                        ? new BraceSpan( begSpan, head.LastTokenIndex + 1 )
                        : null;
            }
            if( t.TokenType is TokenType.EndOfInput )
            {
                return null;
            }
            if( t is not TokenError )
            {
                analyzer.HandleKnownSpan( ref head, t );
            }
        }
    }

}
