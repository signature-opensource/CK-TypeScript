using CK.Core;
using System.Collections.Generic;

namespace CK.Transform.Core;

public sealed class FilteredTokenSpanListBuilder
{
    readonly List<FilteredTokenSpan> _list;
    int _each;
    int _match;

    public FilteredTokenSpanListBuilder()
    {
        _list = new List<FilteredTokenSpan>();
    }

    public void AddMatch( TokenSpan span )
    {
        if( _list.Count > 0 )
        {
            var last = _list[^1].Span;
            Throw.CheckArgument( last.GetRelationship( span ) is SpanRelationship.Independent or SpanRelationship.Contiguous );
        }
        _list.Add( new FilteredTokenSpan( _each, _match++, span ) );
    }

    public int CurrentEachCount => _each;

    public int CurrentMatchCount => _match;

    public void StartNewEach()
    {
        if( _match != 0 )
        {
            ++_each;
            _match = 0;
        }
    }

    public void Clear()
    {
        _list.Clear();
        _each = 0;
        _match = 0;
    }

    public FilteredTokenSpan[] ExtractResult()
    {
        var m = _list.ToArray();
        Clear();
        return m;
    }
}


