using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
    /// <summary>
    /// Validates these filtered tokens.
    /// </summary>
    /// <param name="matches">These filtered tokens.</param>
    /// <param name="tokens">
    /// Optional tokens to which these filtered tokens refer.
    /// When provided, this is used to check that the last span ends on or before the last token.
    /// </param>
    /// <returns>True if these filtered tokens are valid, false otherwise.</returns>
    public static bool CheckValid( this IReadOnlyList<FilteredTokenSpan> matches, IReadOnlyList<Token>? tokens )
    {
        return GetError( matches, tokens ) != null;
    }

    /// <summary>
    /// Validates these filtered tokens.
    /// </summary>
    /// <param name="matches">These filtered tokens.</param>
    /// <param name="tokens">
    /// Optional tokens to which these filtered tokens refer.
    /// When provided, this is used to check that the last span ends on or before the last token.
    /// </param>
    /// <param name="error">On error, contains a description of the error.</param>
    /// <returns>True if these filtered tokens are valid, false otherwise.</returns>
    public static bool CheckValid( this IReadOnlyList<FilteredTokenSpan> matches,
                                   IReadOnlyList<Token>? tokens,
                                   [NotNullWhen(true)]out string? error )
    {
        error = GetError( matches, tokens );
        return error != null;
    }

    static string? GetError( IReadOnlyList<FilteredTokenSpan> matches, IReadOnlyList<Token>? tokens )
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
                if( each != expectedEach )
                {
                    return $"Invalid EachNumber at {i}. Expected {expectedEach}, got {each}.";
                }

                if( match != expectedMatch )
                {
                    return $"Invalid MatchNumber at {i}. Expected {expectedMatch}, got {match}.";
                }

                var span = matches[i].Span;
                var nextSpan = matches[i + 1].Span;
                if( span.IsEmpty )
                {
                    return $"Empty span found at {i}.";
                }
                if( span.GetRelationship( nextSpan ) is not SpanRelationship.Independent and not SpanRelationship.Contiguous )
                {
                    return $"Span at {i} ({span}) overlaps the next one {nextSpan}.";
                }
            }
            if( tokens != null )
            {
                if( matches[^1].Span.End > tokens.Count )
                {
                    return $"Last span {matches[^1].Span} ends after the last token: token count is {tokens.Count}.";
                }
            }
        }
        return null; 
    }
}
