using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// A <see cref="ITokenFilterOperator"/> that extends a matched range to
/// the deepest span that can be assigned to a type.
/// </summary>
public sealed class SingleSpanTypeFilter : ITokenFilterOperator
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
    public void Activate( Action<ITokenFilterOperator> collector ) => collector( this );

    public void Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        var builder = context.SharedBuilder;
        var spanCollector = new DeepestSpanCollector();
        var e = input.CreateTokenEnumerator();
        while( e.NextEach() )
        {
            while( e.NextMatch() )
            {
                while( e.NextToken() )
                {
                    var s = context.GetDeepestSpanAt( e.Token.Index, _spanType );
                    if( s != null ) spanCollector.Add( s.Span );
                }
            }
            if( !spanCollector.ExtractResultTo( builder ) )
            {
                context.SetFailedResult( "Missing span.", e );
                return;
            }
        }
        context.SetResult( builder );
    }


    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable ) b.Append( "[SpanType] " );
        return b.Append( _displayName );
    }

    public override string ToString() => _displayName;


    readonly struct DeepestSpanCollector
    {
        readonly List<TokenSpan> _spans;

        public DeepestSpanCollector()
        {
            _spans = new List<TokenSpan>();
        }

        public bool ExtractResultTo( TokenFilterBuilder builder )
        {
            if( _spans.Count == 0 ) return false;
            builder.StartNewEach();
            foreach( var s in _spans )
            {
                builder.AddMatch( s );
            }
            return true;
        }

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

}
