using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// (first [+ n] [out of n] 
/// | last [- n] [out of n] 
/// | single) <see cref="INodeLocationPatternNode"/> 
/// </summary>
public sealed class SingleLocationFinder : CompositeNode, INodeLocationFinderNode
{
    static readonly RequiredChild<TokenNode> _firstOrLastOrSingle = new( 0 );
    static readonly OptionalChild<TokenNode> _plusOrMinus = new( 1 );
    static readonly OptionalChild<TokenNode> _offset = new( 2 );
    static readonly OptionalChild<TokenNode> _outT = new( 3 );
    static readonly OptionalChild<TokenNode> _ofT = new( 4 );
    static readonly OptionalChild<TokenNode> _expectedMatchCountT = new( 5 );
    static readonly RequiredChild<INodeLocationPatternNode> _pattern = new( 6 );

    LocationCardinalityInfo _cardinalityInfo;

    internal SingleLocationFinder( TokenNode firstOrLastOrSingle,
                                   TokenNode? plusOrMinus,
                                   TokenNode? offset,
                                   TokenNode? outT,
                                   TokenNode? ofT,
                                   TokenNode? expectedMatchCount,
                                   INodeLocationPatternNode pattern )
           : base( firstOrLastOrSingle, plusOrMinus, offset, outT, ofT, expectedMatchCount, Unsafe.As<AbstractNode>( pattern ) )
    {
    }

    protected override void DoCheckInvariants( int storeLength )
    {
        Throw.CheckArgument( storeLength == 7 );
        _firstOrLastOrSingle.Check( this, "FirstOrLastOrSingle" );
        _plusOrMinus.Check( this, "PlusOrMinus" );
        _offset.Check( this, "Offset" );
        _outT.Check( this, "Out" );
        _ofT.Check( this, "Of" );
        _expectedMatchCountT.Check( this, "ExpectedMatchCount" );
        _pattern.Check( this, "Pattern" );
        _cardinalityInfo = ComputeCardinalityInfo();
    }

    TokenNode FirstOrLastOrSingleT => _firstOrLastOrSingle.Get( this );

    TokenNode? PlusOrMinusT => _plusOrMinus.Get( this );

    TokenNode? Offset => _offset.Get( this );

    TokenNode? OutT => _outT.Get( this );

    TokenNode? OfT => _ofT.Get( this );

    TokenNode? ExpectedMatchCount => _expectedMatchCountT.Get( this );

    /// <inheritdoc />
    public LocationCardinalityInfo GetCardinality() => _cardinalityInfo.IsValid ? _cardinalityInfo : (_cardinalityInfo = ComputeCardinalityInfo());

    /// <inheritdoc />
    public INodeLocationPatternNode Pattern => _pattern.Get( this );

    LocationCardinalityInfo ComputeCardinalityInfo()
    {
        bool isSingle = FirstOrLastOrSingleT.Text.Span.Equals( "single", StringComparison.Ordinal );
        bool isFirst = FirstOrLastOrSingleT.Text.Span.Equals( "first", StringComparison.Ordinal );
        bool isLast = FirstOrLastOrSingleT.Text.Span.Equals( "last", StringComparison.Ordinal );
        if( !isSingle && !isFirst && !isLast )
        {
            throw new ArgumentException( $"Must be 'single', 'first' or 'last', not '{FirstOrLastOrSingleT.Text.Span}'." );
        }
        if( isSingle )
        {
            if( Offset != null || PlusOrMinusT != null ) throw new ArgumentException( "Invalid offset after 'single'." );
        }
        else if( Offset != null )
        {
            if( isLast )
            {
                if( PlusOrMinusT == null || !PlusOrMinusT.Text.Span.Equals( "-", StringComparison.Ordinal ) )
                {
                    throw new ArgumentException( "'last' offset requires a minus sign: 'last - 2'." );
                }
            }
            else
            {
                if( PlusOrMinusT == null || !PlusOrMinusT.Text.Span.Equals( "+", StringComparison.Ordinal ) )
                {
                    throw new ArgumentException( "'first' offset requires a plus sign: 'first + 1'." );
                }
            }
        }
        if( (OutT != null) != (OfT != null) || (OutT != null) != (ExpectedMatchCount != null) )
        {
            throw new ArgumentException( $"Out, Of and ExpectedMatchCount must all be null or not null." );
        }
        int offset = Offset == null ? 0 : int.Parse( Offset.Text.Span );
        if( offset < 0 )
        {
            throw new ArgumentException( "offset must be positive." );
        }
        int expectedMatchCount = ExpectedMatchCount == null ? 0 : int.Parse( ExpectedMatchCount.Text.Span );
        if( expectedMatchCount < 0 )
        {
            throw new ArgumentException( "expectedMatchCount must be positive." );
        }
        return new LocationCardinalityInfo( isSingle, isFirst, offset, expectedMatchCount );
    }

    SingleLocationFinder( ImmutableArray<Trivia> leading, CompositeNodeMutator content, ImmutableArray<Trivia> trailing )
    : base( leading, content, trailing )
    {
    }

    protected internal override AbstractNode DoClone( ImmutableArray<Trivia> leading, CompositeNodeMutator content, ImmutableArray<Trivia> trailing )
    {
        return new SingleLocationFinder( leading, content, trailing );
    }
}
