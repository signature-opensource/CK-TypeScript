using CK.Core;
using System;
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
        foreach( var each in inner )
        {
            foreach( var range in each )
            {
                using var e = range.GetEnumerator();
                if( e.MoveNext() )
                {
                    var s = c.GetDeepestSpanAt( e.Current.Index, _spanType );
                    if( s != null )
                    {
                        while( e.MoveNext() )
                        {
                            if( e.Current.Index >= s.Span.End ) continue;
                        }
                        yield return [c.GetSourceTokens( s )];
                    }
                }
            }
        }
    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable ) b.Append( "[SpanType] " );
        return b.Append( _displayName );
    }

    public override string ToString() => _displayName;

}
