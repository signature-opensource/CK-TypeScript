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
    /// The span can be the same as the last added one or must be after it otherwise a <see cref="ArgumentException"/> is thrown.
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
            if( relationshipWithLast is SpanRelationship.Equal  )
            {
                return;
            }
            if( relationshipWithLast is not SpanRelationship.Independent and not SpanRelationship.Contiguous )
            {
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
    /// Starts a new "each" bucket.
    /// <para>
    /// When <paramref name="skipEmpty"/> is true, if no match has been added to the current "each"
    /// (<see cref="CurrentMatchNumber"/> is 0) nothing is done.
    /// </para>
    /// <para>
    /// When <paramref name="skipEmpty"/> is false, if no match has been added to the current "each"
    /// a <see cref="TokenMatch.IsEmpty"/> is added to the current "each" and a new "each" is opened.
    /// </para>
    /// </summary>
    /// <param name="skipEmpty">
    /// True to add a <see cref="TokenMatch.IsEmpty"/> match if none has been added,
    /// false to keep the <see cref="CurrentEachNumber"/> unchanged.
    /// </param>
    public void StartNewEach( bool skipEmpty )
    {
        if( _match != 0 )
        {
            ++_each;
            _match = 0;
        }
        else if( !skipEmpty )
        {
            _list.Add( new TokenMatch( _each, 0, TokenSpan.Empty ) );
            ++_each;
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

