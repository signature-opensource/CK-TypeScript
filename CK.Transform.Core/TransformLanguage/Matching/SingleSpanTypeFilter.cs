using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

/// <summary>
/// A <see cref="IFilteredTokenEnumerableProvider"/> that extends a matched range to
/// the deepest span that can be assigned to a type.
/// </summary>
public sealed class SingleSpanTypeFilter : IFilteredTokenEnumerableProvider
{
    readonly Type _spanType;

    /// <summary>
    /// Initializes a new <see cref="SingleSpanTypeFilter"/>.
    /// </summary>
    /// <param name="spanType">The span type to consider.</param>
    public SingleSpanTypeFilter( Type spanType )
    {
        _spanType = spanType;
    }

    /// <summary>
    /// Gets the projection.
    /// </summary>
    /// <returns>The filtered token projection.</returns>
    public Func<TokenFilterBuilderContext,
                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> GetFilteredTokenProjection()
    {
        return Run;
    }

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Run( TokenFilterBuilderContext c,
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


}
