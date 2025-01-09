using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace CK.Transform.Core;

/// <summary>
/// (all|each) [n] <see cref="INodeLocationPatternNode"/>
/// </summary>
public sealed class MultiLocationFinder : CompositeNode, INodeLocationFinderNode
{
    static readonly RequiredChild<TokenNode> _allOrEach = new( 0 );
    static readonly OptionalChild<TokenNode> _expectedMatchCountT = new( 1 );
    static readonly RequiredChild<INodeLocationPatternNode> _pattern = new( 2 );

    LocationCardinalityInfo _cardinalityInfo;

    public MultiLocationFinder( TokenNode allOrEach, TokenNode expectedMatchCount, INodeLocationPatternNode pattern )
           : base( allOrEach, expectedMatchCount, Unsafe.As<AbstractNode>( pattern ) )
    {
    }

    protected override void DoCheckInvariants( int storeLength )
    {
        Throw.CheckArgument( storeLength == 2 );
        _allOrEach.Check( this, "FirstOrLastOrSingle" );
        _expectedMatchCountT.Check( this, "ExpectedMatchCount" );
        _pattern.Check( this, "Pattern" );
        _cardinalityInfo = ComputeCardinalityInfo();
    }

    TokenNode AllOrEachT => _allOrEach.Get( this );

    TokenNode? ExpectedMatchCount => _expectedMatchCountT.Get( this );

    /// <inheritdoc />
    public INodeLocationPatternNode Pattern => _pattern.Get( this );

    /// <inheritdoc />
    public LocationCardinalityInfo GetCardinality() => _cardinalityInfo.IsValid ? _cardinalityInfo : (_cardinalityInfo = ComputeCardinalityInfo());

    LocationCardinalityInfo ComputeCardinalityInfo()
    {
        bool isAll = AllOrEachT.Text.Span.Equals( "all", StringComparison.Ordinal );
        bool isEach = AllOrEachT.Text.Span.Equals( "each", StringComparison.Ordinal );
        if( !isAll && !isEach )
        {
            throw new ArgumentException( $"Must be 'all' or 'each', not '{AllOrEachT.Text.Span}'." );
        }
        int expectedMatchCount = ExpectedMatchCount == null ? 0 : int.Parse( ExpectedMatchCount.Text.Span );
        if( expectedMatchCount < 0 )
        {
            throw new ArgumentException( "expectedMatchCount must be positive." );
        }
        return new LocationCardinalityInfo( isAll, expectedMatchCount );
    }

    MultiLocationFinder( ImmutableArray<Trivia> leading, CompositeNodeMutator content, ImmutableArray<Trivia> trailing )
        : base( leading, content, trailing )
    {
    }

    protected internal override AbstractNode DoClone( ImmutableArray<Trivia> leading, CompositeNodeMutator content, ImmutableArray<Trivia> trailing )
    {
        return new MultiLocationFinder( leading, content, trailing );
    }

}
