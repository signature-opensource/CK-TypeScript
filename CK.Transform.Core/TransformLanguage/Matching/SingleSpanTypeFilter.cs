using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// A <see cref="IFilteredTokenOperator"/> that extends a matched range to
/// the deepest span that can be assigned to a type.
/// </summary>
public sealed class SingleSpanTypeFilter : IFilteredTokenOperator
{
    readonly Type _spanType;
    readonly string _displayName;

    /// <summary>
    /// Initializes a new <see cref="SingleSpanTypeFilter"/>.
    /// </summary>
    /// <param name="spanType">The span type to consider.</param>
    /// <param name="displayName">The span type name to display.</param>
    public SingleSpanTypeFilter( Type spanType, string displayName )
    {
        _spanType = spanType;
        _displayName = displayName;
    }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string DisplayName => _displayName;

    /// <summary>
    /// Collects this operator.
    /// </summary>
    /// <param name="collector">The operator collector.</param>
    public void Activate( Action<IFilteredTokenOperator> collector ) => collector( this );

    public FilteredTokenSpan[] Apply( IFilteredTokenOperatorContext context, IReadOnlyList<FilteredTokenSpan> input )
    {

        throw new NotImplementedException();
    }


    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Run( IFilteredTokenOperatorContext c,
                                                            IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        DeepestSpanCollector collector = new DeepestSpanCollector( c );
        foreach( var each in inner )
        {
            collector.StartEach();
            foreach( var range in each )
            {
                foreach( var t in range )
                {
                    var s = c.GetDeepestSpanAt( t.Index, _spanType );
                    if( s != null ) collector.Add( s );
                }
            }
        }
        collector.Close();
        return collector;
    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable ) b.Append( "[SpanType] " );
        return b.Append( _displayName );
    }

    public override string ToString() => _displayName;

    sealed class DeepestSpanCollector : IEnumerable<IEnumerable<IEnumerable<SourceToken>>>
    {
        readonly List<List<SourceSpan>> _each;
        readonly IFilteredTokenOperatorContext _filterContext;
        List<SourceSpan>? _current;

        public DeepestSpanCollector( IFilteredTokenOperatorContext filterContext )
        {
            _each = new List<List<SourceSpan>>();
            _filterContext = filterContext;
        }

        public void StartEach()
        {
            Close();
            _current = new List<SourceSpan>();
        }

        public void Close()
        {
            if( _current != null )
            {
                _each.Add( _current );
            }
        }

        public void Add( SourceSpan span )
        {
            Throw.DebugAssert( _current != null );

            for( int i = 0; i < _current.Count; i++ )
            {
                SourceSpan? s = _current[i];
                if( s.Span.Contains( span.Span ) )
                {
                    _current[i] = span;
                    return;
                }
                if( span.Span.ContainsOrEquals( s.Span ) )
                {
                    return;
                }
            }
            _current.Add( span );
        }

        public IEnumerator<IEnumerable<IEnumerable<SourceToken>>> GetEnumerator()
        {
            foreach( var each in _each )
            {
                yield return each.Select( _filterContext.GetSourceTokens );
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
