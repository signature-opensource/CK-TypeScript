using System.Collections.Generic;

namespace CK.Transform.Core;

readonly struct TokenSpanDeepestCollector
{
    readonly List<TokenSpan> _spans;

    public TokenSpanDeepestCollector()
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
    /// Adds a span, keeping the deepest ones.
    /// </summary>
    /// <param name="span">The new span to add.</param>
    public void Add( TokenSpan span )
    {
        for( int i = 0; i < _spans.Count; i++ )
        {
            var s = _spans[i];
            if( s.Contains( span ) )
            {
                _spans[i] = span;
                return;
            }
            if( span.ContainsOrEquals( s ) )
            {
                return;
            }
        }
        _spans.Add( span );
    }

}

