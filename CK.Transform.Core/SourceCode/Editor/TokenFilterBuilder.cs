using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

public sealed class TokenFilterBuilder
{
    readonly List<TokenMatch> _list;
    int _each;
    int _match;

    /// <summary>
    /// Initializes a new empty builder.
    /// </summary>
    public TokenFilterBuilder()
    {
        _list = new List<TokenMatch>();
    }

    /// <summary>
    /// Adds a matched span to the current "each".
    /// The span must not overlap the last added one otherwise a <see cref="ArgumentException"/> is thrown.
    /// </summary>
    /// <param name="span">The span to add.</param>
    public void AddMatch( TokenSpan span )
    {
        if( _list.Count > 0 )
        {
            var last = _list[^1].Span;
            var relationshipWithLast = last.GetRelationship( span );
            // The regular case is that the added span must be
            // after or contiguous to the last one.
            if( relationshipWithLast is not SpanRelationship.Independent and not SpanRelationship.Contiguous )
            {
                // If we have already entered a new "each" bucket (_match > 0), the added span MUST be
                // after or contiguous to the last one.
                //
                // But if we are on the first span of a new "each" bucket (_match == 0):
                // - If the initial span of the new "each" is the same as the previous one
                //   we silently ignore it: the previous each wins (as it is the first one).
                // - If the span is before the last one, this is an error: the calling operators must
                //   definitly not do this. It must be fixed.
                // - If the span overlaps the last one, we could kindly truncate the new span so that
                //   it starts right after the last one.
                //   This is weird: the span doesn't cover what the operator decided, this change the span semantics.
                //   This currently seems dangerous to allow this. If required, we'll add a bool allowTruncateInitialEachSpan = false
                //   parameter to this method (or to the constructor).
                //
                if( _match == 0 && relationshipWithLast is SpanRelationship.Equal )
                {
                    return;
                }
                Throw.ArgumentException( nameof(span), $"Added span '{span}' must be after the last one '{last}' (CurrentEachNumber: {_each}, CurrentMatchNumber: {_match})." );
            }
        }
        _list.Add( new TokenMatch( _each, _match++, span ) );
    }

    /// <summary>
    /// Gets the number of "each" bucket.
    /// </summary>
    public int CurrentEachNumber => _each;

    /// <summary>
    /// Gets the number of added matches in the current "each" bucket.
    /// </summary>
    public int CurrentMatchNumber => _match;

    /// <summary>
    /// Starts a new "each" bucket. If no match has been added to the current "each"
    /// (<see cref="CurrentMatchNumber"/> is 0) nothing is done.
    /// </summary>
    public void StartNewEach()
    {
        if( _match != 0 )
        {
            ++_each;
            _match = 0;
        }
    }

    /// <summary>
    /// Clears this builder. It can be reused.
    /// </summary>
    public void Clear()
    {
        _list.Clear();
        _each = 0;
        _match = 0;
    }

    /// <summary>
    /// Extracts the current result and <see cref="Clear"/> this builder.
    /// </summary>
    /// <returns>The matches.</returns>
    public TokenMatch[] ExtractResult()
    {
        var m = _list.ToArray();
        Clear();
        return m;
    }
}

