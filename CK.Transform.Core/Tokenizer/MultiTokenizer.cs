using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.Core;

sealed class MultiTokenizer : Tokenizer
{
    readonly List<PartialTokenizer> _tokenizers;

    public MultiTokenizer()
    {
        _tokenizers = new List<PartialTokenizer>();
    }

    public IReadOnlyList<PartialTokenizer> PartialTokenizers => _tokenizers;

    public void AddTokenizer( PartialTokenizer tokenizer )
    {
        Throw.CheckArgument( tokenizer != null && tokenizer.Host is null );
        _tokenizers.Add( tokenizer );
        tokenizer.SetHost( this );
    }

    public void InsertTokenizer( int index, PartialTokenizer tokenizer )
    {
        Throw.CheckArgument( tokenizer != null && tokenizer.Host is null );
        _tokenizers.Insert( index, tokenizer );
        tokenizer.SetHost( this );
    }

    public void RemoveTokenizer( PartialTokenizer tokenizer )
    {
        Throw.CheckArgument( tokenizer != null && tokenizer.Host == this );
        _tokenizers.Remove( tokenizer );
        tokenizer.SetHost( null );
    }

    internal protected override TokenNode Tokenize( ImmutableArray<Trivia> leadingTrivias, ref ReadOnlyMemory<char> head )
    {
        foreach( var p in _tokenizers )
        {
            if( p.IsDisabled ) continue;
            var t = p.Tokenize( leadingTrivias, ref head );
            if( t != TokenErrorNode.Unhandled )
            {
                return t;
            }
        }
        var active = _tokenizers.Where( p => !p.IsDisabled ).Select( p => p.Name ).Concatenate();
        return new TokenErrorNode( TokenType.ErrorUnhandled, $"Unrecognized token by {_tokenizers.Count} PartialTokenizers: {active}.", leadingTrivias );
    }
}
