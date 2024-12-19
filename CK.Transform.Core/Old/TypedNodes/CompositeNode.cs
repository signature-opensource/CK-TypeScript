using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CK.Transform.Core;

/// <summary>
/// A composite node contains an oredered list of typed children, some of them
/// being optional (nullable).
/// <para>
/// There is no <see cref="AbstractNode.DoCheckInvariants(int)"/> implementation at this level: it must
/// be provided by the concrete node (and overridden by any specialization). There is no invariant
/// "by default". The pathological case of an empty composite without any <see cref="ChildrenNodes"/>
/// is technically possible (but should of course be avoided).
/// </para>
/// <para>
/// A typical DoCheckInvariants implementation should check the expected store length the
/// access to its children by calling <see cref="At{T}(int)"/> and check any condition on them.
/// </para>
/// </summary>
public abstract partial class CompositeNode : SyntaxNode
{
    internal readonly AbstractNode?[] _store;
    AbstractNode[]? _content;

    /// <summary>
    /// Regular constructor with the list of types children nodes.
    /// </summary>
    /// <param name="uncheckedStore">The children nodes.</param>
    protected CompositeNode( params AbstractNode?[] uncheckedStore )
        : base( ImmutableArray<Trivia>.Empty, ImmutableArray<Trivia>.Empty )
    {
        _store = uncheckedStore;
    }

    /// <summary>
    /// Regular constructor with the list of types children nodes.
    /// </summary>
    /// <param name="leading">The leading trivia.</param>
    /// <param name="trailing">The trailing trivia.</param>
    /// <param name="uncheckedStore">The children nodes.</param>
    protected CompositeNode( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing, params AbstractNode?[] uncheckedStore )
        : base( leading, trailing )
    {
        _store = uncheckedStore;
    }

    /// <summary>
    /// Constructor for <see cref="DoClone(ImmutableArray{Trivia}, CompositeNodeMutator, ImmutableArray{Trivia})"/>.
    /// </summary>
    /// <param name="leading">The leading trivia.</param>
    /// <param name="content">The mutated content.</param>
    /// <param name="trailing">The trailing trivia.</param>
    protected CompositeNode( ImmutableArray<Trivia> leading, CompositeNodeMutator content, ImmutableArray<Trivia> trailing )
        : base( leading, trailing )
    {
        _store = content.RawItems;
    }

    /// <summary>
    /// Overridden to call <see cref="DoCheckInvariants(int)"/> that is the
    /// actual implementation.
    /// <para>
    /// This checks that no children is a <see cref="ErrorTolerant.IErrorTolerantNode"/>.
    /// </para>
    /// </summary>
    protected override sealed void DoCheckInvariants()
    {
        Throw.CheckArgument( _store.All( c => c is not ErrorTolerant.IErrorTolerantNode ) );
        DoCheckInvariants( _store.Length );
    }

    /// <summary>
    /// Should check the <paramref name="storeLength"/> and at least access all the
    /// items with <see cref="At{T}(int)"/>: this basically validates
    /// the store.
    /// <para>
    /// Should throw on any invalid store content.
    /// </para>
    /// </summary>
    /// <param name="storeLength">The store length.</param>
    protected abstract void DoCheckInvariants( int storeLength );

    /// <summary>
    /// Gets the nullable node from the internal composite store.
    /// <para>
    /// Unlike <see cref="UnsafeAt{T}(int)"/>, this is "safe": it throws an <see cref="ArgumentOutOfRangeException"/>
    /// or a <see cref="InvalidCastException"/> on error.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Type of the node.</typeparam>
    /// <param name="index">Index in the store.</param>
    /// <returns>The node.</returns>
    protected T? At<T>( int index ) where T : class, IAbstractNode => (T?)(object?)_store[index];

    static void SafeCheck( Action<CompositeNode,int> a, CompositeNode n, int index, string propertName )
    {
        try
        {
            a( n, index );
        }
        catch( Exception ex )
        {
            throw new ArgumentException( $"Invalid child content in '{n.GetType().Name}' for '{propertName}' at {index}.", ex );
        }
    }

    protected readonly struct OptionalChild<T> where T : class, IAbstractNode
    {
        readonly int _index;

        public int Index => _index;

        public OptionalChild( int index ) => _index = index;

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public T? Get( CompositeNode n ) => n.UnsafeAt<T>( _index );

        public T? Check( CompositeNode n, string propertName, Action<T>? moreChecks = null )
        {
            SafeCheck( ( n, i ) =>
            {
                var c = n.At<T>( i );
                if( c != null && moreChecks != null ) moreChecks( c );
            }, n, _index, propertName );
            return n.UnsafeAt<T>( _index )!;
        }
    }

    protected readonly struct RequiredChild<T> where T : class, IAbstractNode
    {
        readonly int _index;
        public RequiredChild( int index ) => _index = index;

        public int Index => _index;

        public T Check( CompositeNode n, string propertName, Action<T>? moreChecks = null )
        {
            SafeCheck( ( n, i ) =>
            {
                var child = n.At<T>( i );
                Throw.CheckNotNullArgument( child );
                moreChecks?.Invoke( child );
            }, n, _index, propertName );
            return n.UnsafeAt<T>( _index )!;
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public T Get( CompositeNode n ) => n.UnsafeAt<T>( _index )!;
    }

    /// <summary>
    /// Gets the nullable node from the internal composite store.
    /// <para>
    /// This is an unsafe (but almost fastest implementation) and could lead
    /// to Access Violation (AV) and trigger an immediate crash of the whole
    /// process.
    /// </para>
    /// <para>
    /// This must be called ONLY on fully validated store content (a fully valid
    /// <see cref="AbstractNode.DoCheckInvariants()"/> should have been called or you are absoltely sure
    /// of what you are doing).
    /// Use it a your own risk.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Type of the node.</typeparam>
    /// <param name="index">Index in the store.</param>
    /// <returns>The node.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    protected T? UnsafeAt<T>( int index ) where T : class, IAbstractNode
    {
        ref AbstractNode? first = ref MemoryMarshal.GetArrayDataReference( _store );
        return Unsafe.As<T?>( Unsafe.Add( ref first, (nint)index ) );
    }

    /// <inheritdoc />
    public override IReadOnlyList<AbstractNode> ChildrenNodes => _content ??= CreateChildren( _store );

    static AbstractNode[] CreateChildren( AbstractNode?[] store )
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

    /// <inheritdoc />
    public override CompositeNodeMutator CreateMutator() => new CompositeNodeMutator( this );

    /// <summary>
    /// Fundamental method that rebuilds this Node with a mutated content.
    /// <see cref="AbstractNode.CheckInvariants"/> is automatically called on the resu
    /// <para>
    /// This is called only if a mutation is required because the content has changed (Trivias mutations
    /// are handled independently).
    /// </para>
    /// <para>
    /// This method is allowed to return a different node type than this one. This allows mutations to be handled
    /// at the node level, not only at the parent/child seam.
    /// </para>
    /// </summary>
    /// <param name="leading">Leading trivias.</param>
    /// <param name="content">New content to handle.</param>
    /// <param name="trailing">Trailing trivias.</param>
    /// <returns>A new immutable object.</returns>
    internal protected abstract AbstractNode DoClone( ImmutableArray<Trivia> leading, CompositeNodeMutator content, ImmutableArray<Trivia> trailing );

}

