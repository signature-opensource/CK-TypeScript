using CK.Core;

namespace CK.Transform.Core;

/// <summary>
/// Base class for visitors. Support both read-only and mutation API.
/// </summary>
public class BaseNodeVisitor
{
    /// <summary>
    /// Mutation visit entry point that calls the protected virtual <see cref="VisitItem"/>.
    /// </summary>
    /// <param name="root">Root node to visit.</param>
    /// <returns>The root node or a transformed one.</returns>
    public virtual AbstractNode? VisitRoot( IAbstractNode root ) => VisitItem( root );

    /// <summary>
    /// Protected entry point that can be overridden.
    /// <para>
    /// This routes the call to <see cref="VisitToken(TokenNode)"/>, <see cref="VisitCollection(CollectionNode)"/>
    /// or <see cref="VisitComposite(CompositeNode)"/>.
    /// There is little to no reason to change this.
    /// </para>
    /// </summary>
    /// <param name="e">The node to visit.</param>
    /// <returns>The resulting node.</returns>
    protected virtual AbstractNode? VisitItem( IAbstractNode e )
    {
        return e switch
        {
            TokenNode t => VisitToken( t ),
            CollectionNode c => VisitCollection( c ),
            CompositeNode n => VisitComposite( n ),
            _ => Throw.NotSupportedException<AbstractNode?>()
        };
    }

    /// <summary>
    /// Visits a <see cref="TokenNode"/>.
    /// Returns the visited token at this level.
    /// </summary>
    /// <param name="t">The visited token.</param>
    /// <returns>The visit result.</returns>
    protected virtual AbstractNode? VisitToken( TokenNode t ) => t;

    /// <summary>
    /// Visits a <see cref="CollectionNode"/>.
    /// </summary>
    /// <param name="t">The visited node.</param>
    /// <returns>The visit result.</returns>
    protected virtual AbstractNode? VisitCollection( CollectionNode c )
    {
        var mutator = c.CreateMutator();
        mutator.ApplyMutation( VisitItem );
        return mutator.Clone();
    }

    /// <summary>
    /// Visits a <see cref="CompositeNode"/>.
    /// </summary>
    /// <param name="t">The visited node.</param>
    /// <returns>The visit result.</returns>
    protected virtual AbstractNode? VisitComposite( CompositeNode c )
    {
        var mutator = c.CreateMutator();
        mutator.ApplyMutation( VisitItem );
        return mutator.Clone();
    }

    #region VisitReadOnly
    /// <summary>
    /// Visit entry point that calls the protected virtual <see cref="VisitItem"/>.
    /// </summary>
    /// <param name="root">Root node to visit.</param>
    /// <returns>The root node or a transformed one.</returns>
    public virtual void VisitReadOnlyRoot( IAbstractNode root ) => VisitReadOnlyItem( root );

    /// <summary>
    /// Protected entry point that can be overridden.
    /// <para>
    /// This routes the call to <see cref="VisitReadOnlyToken(TokenNode)"/>, <see cref="VisitReadOnlyCollection(CollectionNode)"/>
    /// or <see cref="VisitReadOnlyComposite(CompositeNode)"/>.
    /// There is little to no reason to change this.
    /// </para>
    /// </summary>
    /// <param name="e">The node to visit.</param>
    /// <returns>The resulting node.</returns>
    protected virtual void VisitReadOnlyItem( IAbstractNode e )
    {
        switch( e )
        {
            case TokenNode t: VisitReadOnlyToken( t ); break;
            case CollectionNode c: VisitReadOnlyCollection( c ); break;
            case CompositeNode n: VisitReadOnlyComposite( n ); break;
        };
    }

    /// <summary>
    /// Visits a <see cref="TokenNode"/>.
    /// </summary>
    /// <param name="t">The visited token.</param>
    protected virtual void VisitReadOnlyToken( TokenNode t ) { }

    /// <summary>
    /// Visits a <see cref="CollectionNode"/>.
    /// </summary>
    /// <param name="t">The visited node.</param>
    protected virtual void VisitReadOnlyCollection( CollectionNode c )
    {
        foreach( var o in c._children ) VisitReadOnlyItem( o );
    }

    /// <summary>
    /// Visits a <see cref="CompositeNode"/>.
    /// </summary>
    /// <param name="t">The visited node.</param>
    protected virtual void VisitReadOnlyComposite( CompositeNode c )
    {
        // Use the internal store to avoid concretizing
        // the Children if possible.
        foreach( var o in c._store )
        {
            if( o != null ) VisitReadOnlyItem( o );
        }
    }
    #endregion
}
