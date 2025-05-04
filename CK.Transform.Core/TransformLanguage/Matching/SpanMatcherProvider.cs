using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

/// <summary>
/// Spans that covers one or two tokens: the {spanSepc} "pattern" and is a <see cref="IFilteredTokenEnumerableProvider"/>.
/// <para>
/// The filter can be as complex and language specific as needed. 
/// </para>
/// </summary>
public abstract class SpanMatcherProvider : SourceSpan, IFilteredTokenEnumerableProvider
{
    protected SpanMatcherProvider( int beg, int end )
        : base( beg, end )
    {
        Throw.CheckState( "Must cover at most {spanSepc} \"pattern\" tokens.", Span.Length <= 2 );
    }

    /// <inheritdoc />
    public abstract Func<IActivityMonitor,
                         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
                         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> GetFilteredTokenProjection();
}
