using CK.Core;
using CK.Transform.Core;
using System;

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


    internal static ClassDefinition? Match( TypeScriptAnalyzer analyzer, ref TokenizerHead head, Token classToken )
    {
        Throw.DebugAssert( !head.IsCondemned && head.LastToken == classToken && classToken.TextEquals( "class" ) );
        int begSpan = FindBegSpan( ref head );
        if( !analyzer.SkipTo( ref head, TokenType.OpenBrace ) )
        {
            return null;
        }
        Throw.DebugAssert( head.LastToken?.TokenType is TokenType.OpenBrace );
        if( BraceSpan.Match( analyzer, ref head, head.LastToken ) == null )
        {
            return null;
        }
        return new ClassDefinition( begSpan, head.LastTokenIndex + 1 );

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
