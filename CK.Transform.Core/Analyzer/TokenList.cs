using System.Collections.Generic;

namespace CK.Transform.Core;

public sealed class TokenList
{
    readonly SourceCode _root;
    readonly List<Token> _tokens;

    internal TokenList( SourceCode root )
    {
        _root = root;
        _tokens = new List<Token>();
    }

    public int Count => _tokens.Count;

    public Token this[ int index ] => _tokens[ index ];

    public void Insert( int index, Token token )
    {
        _tokens.Insert( index, token );
        _root.OnInsertToken( index );
    }

    public void RemoveAt( int index )
    {
        _tokens.RemoveAt( index );
        _root.OnRemoveAtToken( index );
    }

    public void RemoveRange( int index, int count )
    {
        _tokens.RemoveRange( index, count );
        _root.OnRemoveRangeToken( index, count );
    }

    public void Append( Token token ) => _tokens.Add( token );

    internal void Append( TokenList other )
    {
        _tokens.AddRange( other._tokens );
    }
}
