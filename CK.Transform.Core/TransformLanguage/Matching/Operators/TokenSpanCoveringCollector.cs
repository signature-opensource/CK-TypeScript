using System.Collections.Generic;

namespace CK.Transform.Core;

readonly struct TokenSpanCoveringCollector
{
    readonly List<TokenSpan> _spans;

    public TokenSpanCoveringCollector()
    {
        _spans = new List<TokenSpan>();
    }

    public bool ExtractResultToNewEach( TokenFilterBuilder builder )
    {
        if( _spans.Count == 0 ) return false;
        builder.StartNewEach();
        foreach( var s in _spans )
        {
            builder.AddMatch( s );
        }
        _spans.Clear();
        return true;
    }

    public void Add( TokenSpan span )
    {
        for( int i = 0; i < _spans.Count; i++ )
        {
            var s = _spans[i];
            if( span.Contains( s ) )
            {
                _spans[i] = span;
                return;
            }
            if( s.ContainsOrEquals( span ) )
            {
                return;
            }
        }
        _spans.Add( span );
    }

}

