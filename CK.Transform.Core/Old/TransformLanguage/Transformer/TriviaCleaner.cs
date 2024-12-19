using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Immutable;

namespace CK.Transform.TransformLanguage;


/// <summary>
/// Transformer that removes trivias.
/// </summary>
public class TriviaCleaner : TransformVisitor
{
    readonly bool _clearBlockcomment;
    readonly bool _clearLineComment;
    readonly bool _innerTriviasOnly;
    readonly ImmutableArray<Trivia>.Builder _builder;

    /// <summary>
    /// Initializes a new <see cref="TriviaCleaner"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="clearLineComment">True to remove line comments.</param>
    /// <param name="clearBlockComment">True to remove block comments.</param>
    /// <param name="innerTriviasOnly">
    /// True to remove only trivia that are inside the current selected range.
    /// False to also remove leading and trailing ones.
    /// </param>
    /// <exception cref="ArgumentException"></exception>
    public TriviaCleaner( IActivityMonitor monitor, bool clearLineComment, bool clearBlockComment, bool innerTriviasOnly )
        : base( monitor )
    {
        Throw.CheckArgument( clearLineComment || clearBlockComment );
        _clearLineComment = clearLineComment;
        _clearBlockcomment = clearBlockComment;
        _innerTriviasOnly = innerTriviasOnly;
        _builder = ImmutableArray.CreateBuilder<Trivia>();
    }

    /// <summary>
    /// This transformer never returns null.
    /// </summary>
    /// <param name="root">The root node to visit.</param>
    /// <returns>The visited result.</returns>
    public new AbstractNode VisitRoot( IAbstractNode root ) => VisitRoot( root, rangeFilter: null )!;

    /// <summary>
    /// Removes the trivias.
    /// </summary>
    /// <param name="e">The visited node.</param>
    /// <returns>The resulting node.</returns>
    protected override AbstractNode AfterVisitItem( AbstractNode e )
    {
        var leading = _innerTriviasOnly && VisitContext.RangeFilterStatus.IsBegAfter()
        ? Remove( e.LeadingTrivias )
                        : e.LeadingTrivias;
        var trailing = _innerTriviasOnly && VisitContext.RangeFilterStatus.IsEndBefore()
                        ? Remove( e.TrailingTrivias )
                        : e.TrailingTrivias;
        return e.SetTrivias( leading, trailing );
    }

    ImmutableArray<Trivia> Remove( ImmutableArray<Trivia> trivias )
    {
        if( !trivias.IsEmpty )
        {
            _builder.Clear();
            bool hasEOL = false;
            foreach( var t in trivias )
            {
                if( t.IsLineComment && _clearLineComment )
                {
                    if( !hasEOL )
                    {
                        _builder.Add( new Trivia( TokenType.Whitespace, Environment.NewLine ) );
                        hasEOL = true;
                    }
                }
                else if( t.IsBlockComment && _clearBlockcomment )
                {
                    _builder.Add( new Trivia( TokenType.Whitespace, " " ) );
                }
                else
                {
                    hasEOL = false;
                    _builder.Add( t );
                }
            }
            trivias = _builder.DrainToImmutable();
        }
        return trivias;
    }
}
