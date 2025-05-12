using CK.Core;
using System.Collections.Generic;

namespace CK.Transform.Core;

/// <summary>
/// An array of <c>TokenMatch</c> captures each/match/token structure: tokens are grouped by matches
/// and matches are grouped by each buckets. See <see cref="LocationCardinality.LocationKind.Each"/>.
/// </summary>
/// <param name="EachIndex">The "each" bucket number.</param>
/// <param name="MatchIndex">The match number in the "each" bucket.</param>
/// <param name="Span">The covered token span.</param>
public readonly record struct FilteredTokenSpan( int EachIndex, int MatchIndex, TokenSpan Span );

public static class FilteredTokenSpanExtensions
{
    public static void CheckInvariants( this IReadOnlyList<FilteredTokenSpan> matches, IReadOnlyList<Token>? tokens )
    {
        if( matches.Count > 0 )
        {
            int expectedEach = 0;
            int expectedMatch = 0;
            for( int i = 0; i < matches.Count - 1; ++i )
            {
                int each = matches[i].EachIndex;
                int match = matches[i].MatchIndex;
                if( each != expectedEach )
                {
                    ++expectedEach;
                    expectedMatch = 0;
                }
                else
                {
                    ++expectedMatch;
                }
                Throw.CheckState( each == expectedEach && match == expectedMatch );
                var span = matches[i].Span;
                var nextSpan = matches[i + 1].Span;
                Throw.CheckState( !span.IsEmpty );
                Throw.CheckState( span.GetRelationship( nextSpan ) is SpanRelationship.Independent or SpanRelationship.Contiguous );
            }
            if( tokens != null )
            {
                Throw.CheckState( matches[^1].Span.End <= tokens.Count );
            }
        }
    }
}
