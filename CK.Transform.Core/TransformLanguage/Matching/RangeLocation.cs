using CK.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

/// <summary>
/// Captures "before ...", "after ...", "between ... and ..." where ... is a mono-location
/// <see cref="LocationMatcher"/> (not "each" or "all").
/// </summary>
public sealed class RangeLocation : SourceSpan
{
    Token _kind;

    RangeLocation( int beg, int end, Token kind )
        : base( beg, end )
    {
        _kind = kind;
    }

    /// <summary>
    /// Checks that <see cref="First"/> and <see cref="Second"/> (it is a "between ... and ...") are valid.
    /// </summary>
    /// <returns>True if this span is valid.</returns>
    [MemberNotNullWhen( true, nameof( First ) )]
    public override bool CheckValid()
    {
        if( base.CheckValid() && First != null )
        {
            var k = _kind.Text.Span;
            if( k.Equals( "after", StringComparison.Ordinal ) || k.Equals( "before", StringComparison.Ordinal ) )
            {
                return Second == null;
            }
            return k.Equals( "between", StringComparison.Ordinal ) && Second != null;
        }
        return false;
    }

    /// <summary>
    /// Gets "before", "after" or "between" <see cref="TokenType.GenericIdentifier"/>.
    /// </summary>
    public Token Kind => _kind;

    /// <summary>
    /// Gets whether this is a "after ..." range.
    /// </summary>
    public bool IsAfter => _kind.Text.Span.Equals( "after", StringComparison.Ordinal );

    /// <summary>
    /// Gets whether this is a "before ..." range.
    /// </summary>
    public bool IsBefore => _kind.Text.Span.Equals( "before", StringComparison.Ordinal );

    /// <summary>
    /// Gets whether this is a "between ... and ..." range.
    /// </summary>
    [MemberNotNullWhen( true, nameof( Second ) )]
    public bool IsBetween => Second != null;

    /// <summary>
    /// Gets the first location matcher.
    /// Never null when <see cref="CheckValid()"/> is true.
    /// </summary>
    public LocationMatcher? First => Children.FirstChild as LocationMatcher;

    /// <summary>
    /// Gets the "between ... and " second location.
    /// Not null only when <see cref="IsBetween"/> is true.
    /// </summary>
    public LocationMatcher? Second => Children.FirstChild?.NextSibling as LocationMatcher;

    internal static RangeLocation? Match( TransformerHost.Language language, ref TokenizerHead head )
    {
        int begSpan = head.LastTokenIndex + 1;
        Token? kind;
        LocationMatcher? first;
        LocationMatcher? second = null;
        if( head.TryAcceptToken( "before", out kind ) || head.TryAcceptToken( "after", out kind ) )
        {
            first = LocationMatcher.Parse( language, ref head, monoLocationOnly: true );
            if( first == null )
            {
                return null;
            }
        }
        else if( head.TryAcceptToken( "between", out kind ) )
        {
            first = LocationMatcher.Parse( language, ref head, monoLocationOnly: true );
            if( head.MatchToken( "and" ) is not TokenError )
            {
                second = LocationMatcher.Parse( language, ref head, monoLocationOnly: true );
            }
            if( first == null || second == null )
            {
                return null;
            }
        }
        else
        {
            return null;
        }
        return head.AddSpan( new RangeLocation( begSpan, head.LastTokenIndex + 1, kind ) );
    }
}
