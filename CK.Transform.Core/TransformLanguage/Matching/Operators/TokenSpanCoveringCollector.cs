using CK.Core;
using System.Collections.Generic;

namespace CK.Transform.Core;

readonly struct TokenSpanCoveringCollector
{
    readonly List<TokenSpan> _spans;

    public TokenSpanCoveringCollector()
    {
        _spans = new List<TokenSpan>();
    }

    /// <summary>
    /// Adds all the collected spans to the <see cref="TokenFilterBuilder.AddMatch(TokenSpan)"/>.
    /// </summary>
    /// <param name="builder">The target builder.</param>
    /// <returns>The number of added matches.</returns>
    public int ExtractSpansTo( TokenFilterBuilder builder )
    {
        int count = _spans.Count;
        if( count != 0 )
        {
            foreach( var s in _spans )
            {
                builder.AddMatch( s );
            }
            _spans.Clear();
        }
        return count;
    }

    /// <summary>
    /// Adds a span, keeping the most covering ones.
    /// </summary>
    /// <param name="span">The new span to add.</param>
    public void Add( TokenSpan span )
    {
        int insertIndex = -1;
        bool foundCovered = false;
        for( int i = 0; i < _spans.Count; i++ )
        {
            var s = _spans[i];
            if( span.Contains( s ) )
            {
                if( foundCovered )
                {
                    _spans.RemoveAt( i-- );
                }
                else
                {
                    _spans[i] = span;
                    foundCovered = true;
                }
            }
            if( s.ContainsOrEquals( span ) )
            {
                Throw.DebugAssert( !foundCovered );
                return;
            }
            if( !foundCovered && span.Beg < s.Beg )
            {
                insertIndex = i;
            }
        }
        if( !foundCovered )
        {
            if( insertIndex >= 0 )
            {
                _spans.Insert( insertIndex, span );
            }
            else
            {
                _spans.Add( span );
            }
        }
    }

}

