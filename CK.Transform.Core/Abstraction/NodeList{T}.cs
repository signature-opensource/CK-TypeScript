using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.Core;

public class NodeList<T> : CompositeNode, IAbstractNodeList<T> where T : AbstractNode
{
    readonly IReadOnlyList<T> _items;

    /// <summary>
    /// Initializes a new list.
    /// </summary>
    /// <param name="items">The items of the list.</param>
    /// <param name="leading">The leading trivias.</param>
    /// <param name="trailing">The trailing trivias.</param>
    /// <param name="minCount">Optional minimal item count.</param>
    public NodeList( IEnumerable<T> items, ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing, int minCount = 0 )
        : base( leading, trailing )
    {
        _items = items.ToArray();
        if( _items.Count < minCount ) RaiseItemCountError( this, _items.Count, minCount );
    }

    /// <summary>
    /// Protected constructor that must be used only by <see cref="DoClone(ImmutableArray{Trivia}, IList{AbstractNode}?, ImmutableArray{Trivia})"/>
    /// method. <paramref name="safeContent"/> must be obtained through <see cref="GetSafeContent(IList{AbstractNode}?, int)"/>.
    /// </summary>
    /// <param name="leading">Leading trivias.</param>
    /// <param name="trailing">Trailing trivias.</param>
    /// <param name="safeContent">Items of this list.</param>
    protected NodeList( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing, IReadOnlyList<T> safeContent )
        : base( leading, trailing )
    {
        _items = safeContent;
    }

    static void RaiseItemCountError( AbstractNode o, int count, int minCount )
    {
        Throw.ArgumentException( $"'{o.GetType().Name}': must contain at least {minCount} item(s) (found only {count})." );
    }

    /// <inheritdoc />
    public override IReadOnlyList<AbstractNode> ChildrenNodes => _items;

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <inheritdoc />
    public T this[int index] => _items[index];

    /// <inheritdoc />
    public override IList<AbstractNode> GetRawContent() => _items.Cast<AbstractNode>().ToList();

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// Helper method for <see cref="DoClone(ImmutableArray{Trivia}, IList{AbstractNode}?, ImmutableArray{Trivia})"/> implementations.
    /// This throws ArgumentException if content has null, not T types or if <paramref name="minCount"/> is not satisfied. 
    /// </summary>
    /// <param name="content">The cloned content.</param>
    /// <param name="minCount">Optional minimal count.</param>
    /// <returns>The safe item list to provide to the clone dedicated constructor.</returns>
    protected IReadOnlyList<T> GetSafeContent( IList<AbstractNode>? content, int minCount = 0 )
    {
        IReadOnlyList<T>? safeContent;
        if( content == null )
        {
            safeContent = _items;
        }
        else
        {
            int i = 0;
            foreach( var e in content )
            {
                if( e is not T ) Throw.ArgumentException( $"'{GetType().Name}': Expected item '{typeof( T ).Name}' at {i} but got '{e.GetType().Name ?? "null"}'." );
                ++i;
            }
            if( i < minCount ) RaiseItemCountError( this, i, minCount );
            safeContent = content as IReadOnlyList<T>;
            if( safeContent == null )
            {
                if( i == 0 ) safeContent = Array.Empty<T>();
                else
                {
                    var a = new T[i];
                    i = 0;
                    foreach( var e in content ) a[i++] = (T)e;
                    safeContent = a;
                }
            }
        }
        return safeContent;
    }

    /// <summary>
    /// Clones this list.
    /// <para>
    /// This MUST be overridden by specializations. <see cref="GetSafeContent(IList{AbstractNode}?, int)"/> must be called
    /// before calling <see cref=""/>
    /// </para>
    /// </summary>
    /// <param name="leading">Leading trivias.</param>
    /// <param name="content">New content. Null when unchanged.</param>
    /// <param name="trailing">Trailing trivias.</param>
    /// <returns>A cloned list.</returns>
    protected override AbstractNode DoClone( ImmutableArray<Trivia> leading, IList<AbstractNode>? content, ImmutableArray<Trivia> trailing )
    {
        Throw.CheckState( "The DoClone() method MUST be overridden by specialized types.", GetType() == typeof( NodeList<T> ) );
        return new NodeList<T>( leading, trailing, GetSafeContent( content ) );
    }
}
