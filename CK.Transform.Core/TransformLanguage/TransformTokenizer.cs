using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Immutable;

namespace CK.Transform;

public interface IParser
{
    AbstractNode Parse( ReadOnlyMemory<char> text );
}

public sealed class TransformTokenizer : PartialTokenizer
{
    protected internal override TokenNode Tokenize( ImmutableArray<Trivia> leadingTrivias, ref ReadOnlyMemory<char> head )
    {
        Throw.DebugAssert( "This never called on an empty text.", head.Length > 0 );
        var s = head.Span;
        if( s[0] == '"' )
        {
            return ReadRawString( s, leadingTrivias, ref head );
        }
        if( TryCreateToken( (int)TokenType.Inject, "inject", out var result )
            || TryCreateToken( (int)TokenType.Into, "into", out result ) )
        {
            return result;
        }
        return TokenErrorNode.Unhandled;
    }

    TokenNode ReadRawString( ReadOnlySpan<char> s, ImmutableArray<Trivia> leadingTrivias, ref ReadOnlyMemory<char> head )
    {
        int idx = 0;
        while( ++idx != s.Length && s[idx] == '"' ) ;
        if( idx != s.Length )
        {
            int lineCount = 0;
            int innerStart = idx;
            while( ++idx != s.Length )
            {
                if( )
            }
        }


        throw new NotImplementedException();

        static TokenErrorNode UnterminatedError()
        {
            return new TokenErrorNode( Core.TokenType.ErrorUnterminatedString, "Unterminated raw string" );
        }
    }
}
