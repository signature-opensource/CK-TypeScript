using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Xml.Linq;

namespace CK.Transform.Core;

public abstract class CompositeNode : SyntaxNode
{
    internal readonly AbstractNode?[] _store;
    AbstractNode[]? _content;

    protected CompositeNode( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing, params AbstractNode?[] uncheckedStore )
        : base( leading, trailing )
    {
        _store = uncheckedStore;
    }

    protected CompositeNode( CompositeNode o, ImmutableArray<Trivia> leading, AbstractNode?[]? uncheckedStore, ImmutableArray<Trivia> trailing )
    : base( leading, trailing )
    {
        _store = uncheckedStore ?? o._store;
    }

    public override IReadOnlyList<AbstractNode> ChildrenNodes => _content ??= CreateContent( _store );

    static AbstractNode[] CreateContent( AbstractNode?[] store )
    {
        int count = 0;
        for( var i = 0; i < store.Length; ++i )
        {
            if( store[i] != null ) ++count;
        }
        var c = new AbstractNode[count];
        count = 0;
        for( var i = 0; i < store.Length; ++i )
        {
            var o = store[i];
            if( o != null ) c[count++] = o;
        }
        return c;
    }

    protected override AbstractNode DoClone( ImmutableArray<Trivia> leading, IList<AbstractNode>? content, ImmutableArray<Trivia> trailing )
    {
        throw new NotImplementedException();
    }

    protected abstract CompositeNode DoClone( ImmutableArray<Trivia> leading, MutableCompositeContent? content, ImmutableArray<Trivia> trailing )
    {
        throw new NotImplementedException();
    }
}

public class MutableNodeContent
{
    private protected readonly AbstractNode _node;

    public MutableNodeContent( AbstractNode node )
    {
        _node = node;
        Leading = node.LeadingTrivias;
        Trailing = node.TrailingTrivias;
    }

    public ImmutableArray<Trivia> Leading { get; set; }

    public ImmutableArray<Trivia> Trailing { get; set; }

    AbstractNode Clone()
    {
        if( Trailing == _node.TrailingTrivias )
    }

}

public sealed class MutableCompositeContent : IMutableAbstractNodeContent
{
    readonly CompositeNode _node;
    AbstractNode?[] _content;

    public MutableCompositeContent( CompositeNode node )
    {
        _node = node;
        _content = (AbstractNode?[])_node._store.Clone();
    }

    public ImmutableArray<Trivia> Leading { get; set; }

    public ImmutableArray<Trivia> Trailing { get; set; }

    public void Replace( int index, AbstractNode? node ) => _content[index] = node;

    public int Length => _content.Length;


    public CompositeNode Clone( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing )
    {
        
    }
}


public abstract class CollectionNode : SyntaxNode
{
    internal protected abstract List<AbstractNode> GetMutableContent();
}
