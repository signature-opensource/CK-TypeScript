using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// A <see cref="IFilteredTokenEnumerableProvider"/> that extends a matched range to
/// the deepest span that can be assigned to a type.
/// </summary>
public sealed class SingleSpanTypeFilter : IFilteredTokenEnumerableProvider
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
    /// Collects this provider.
    /// </summary>
    /// <param name="collector">The provider collector.</param>
    public void Activate( Action<IFilteredTokenEnumerableProvider> collector ) => collector( this );

    Func<ITokenFilterBuilderContext,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> IFilteredTokenEnumerableProvider.GetFilteredTokenProjection()
    {
        return Run;
    }


    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Run( ITokenFilterBuilderContext c,
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
        readonly ITokenFilterBuilderContext _filterContext;
        List<SourceSpan>? _current;

        public DeepestSpanCollector( ITokenFilterBuilderContext filterContext )
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
                foreach( var span in each )
                {
                    if( !span.IsDetached )
                    {
                        yield return [_filterContext.GetSourceTokens( span )];
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
