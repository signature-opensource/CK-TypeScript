using CK.Core;
using CK.Transform.Core;

namespace CK.TypeScript.Transform;

/// <summary>
/// Class definition.
/// </summary>
public sealed class ClassDefinition : SourceSpan
{
    ClassDefinition( int beg, int end )
        : base( beg, end )
    {
    }


    internal static ClassDefinition? Match( TypeScriptAnalyzer.Scanner scanner, ref TokenizerHead head, Token classToken )
    {
        Throw.DebugAssert( !head.IsCondemned && head.LastToken == classToken && classToken.TextEquals( "class" ) );
        int begSpan = FindBegSpan( ref head );
        if( !scanner.SkipTo( ref head, TokenType.OpenBrace ) )
        {
            return null;
        }
        Throw.DebugAssert( head.LastToken?.TokenType is TokenType.OpenBrace );
        int expectedDepth = scanner.BraceDepth - 1;
        for(; ; )
        {
            var t = scanner.GetNextToken( ref head );
            if( scanner.BraceDepth == expectedDepth )
            {
                return head.FirstError == null
                        ? head.AddSpan( new ClassDefinition( begSpan, head.LastTokenIndex + 1 ) )
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

        static int FindBegSpan( ref TokenizerHead head )
        {
            Throw.DebugAssert( !head.IsCondemned );
            int backwardIdx = head.LastTokenIndex - 1;
            if( backwardIdx >= 0 )
            {
                if( head.Tokens[backwardIdx].TextEquals( "export" ) )
                {
                    return backwardIdx;
                }
                if( head.Tokens[backwardIdx].TokenType == TokenType.Equals )
                {
                    if( backwardIdx > 0 && head.Tokens[backwardIdx - 1].TokenType == TokenType.GenericIdentifier )
                    {
                        --backwardIdx;
                        // head.Tokens[backwardIdx] is the class name.
                        if( backwardIdx > 0 && head.Tokens[backwardIdx - 1].TextEquals( "static" ) )
                        {
                            --backwardIdx;
                        }
                    }
                    return backwardIdx;
                }
            }
            return head.LastTokenIndex;
        }
    }
}
