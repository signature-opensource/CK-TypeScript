using CK.Core;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.Transform.Core;


public sealed class SourceCode
{
    readonly SourceSpanChildren _children;
    readonly TokenList _tokens;

    public SourceCode()
    {
        _children = new SourceSpanChildren();
        _tokens = new TokenList( this );
    }

    /// <summary>
    /// Gets the children.
    /// </summary>
    public SourceSpanChildren Children => _children;

    /// <summary>
    /// Gets the tokens.
    /// </summary>
    public TokenList Tokens => _tokens;

    /// <summary>
    /// Adds a span or throws if it intersects any existing <see cref="Children"/>.
    /// </summary>
    /// <param name="newOne">The span to add.</param>
    public void Add( SourceSpan newOne )
    {
        Throw.CheckArgument( newOne.IsDetached );
        if( !_children.TryAdd( null, newOne ) )
        {
            Throw.ArgumentException( nameof( newOne ), $"Invalid new '{newOne}'." );
        }
    }

    /// <summary>
    /// Adds a span if it doesn't intersect any existing <see cref="Children"/>.
    /// </summary>
    /// <param name="newOne">The span to add.</param>
    /// <returns>True on success, false if the span intersects an existing span.</returns>
    public bool TryAdd( SourceSpan newOne ) => _children.TryAdd( null, newOne );

    internal void OnInsertToken( int index )
    {
        throw new NotImplementedException();
    }

    internal void OnRemoveAtToken( int index )
    {
        throw new NotImplementedException();
    }

    internal void OnRemoveRangeToken( int index, int count )
    {
        throw new NotImplementedException();
    }
}
