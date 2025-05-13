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
            Throw.CheckArgument( last.GetRelationship( span ) is SpanRelationship.Independent or SpanRelationship.Contiguous );
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

