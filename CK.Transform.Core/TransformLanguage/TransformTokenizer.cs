using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Immutable;

namespace CK.Transform;

public interface IParser
{
    AbstractNode Parse( ReadOnlyMemory<char> text );
}

public sealed class TransformTokenizer : Analyzer
{
    protected override void ParseTrivia( ref TriviaCollector c )
    {
        throw new NotImplementedException();
    }

    protected internal override IAbstractNode? Parse( ImmutableArray<Trivia> leadingTrivias, ref ReadOnlyMemory<char> head )
    {
        Throw.DebugAssert( "This is never called on an empty text.", head.Length > 0 );
        var s = head.Span;
        if( TryCreateToken( (Core.TokenType)TokenType.Inject, "inject", out var inject ) )
        {
            if( !TryCreateToken( (Core.TokenType)TokenType.Into, "into", out var into ) ) return null;
            {
                return into;
            }
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
