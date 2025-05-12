using System;
using System.Collections.Generic;
using static CK.Core.ActivityMonitor;

namespace CK.Transform.Core;

/// <summary>
/// A <see cref="IFilteredTokenSpanEnumerator"/> implementation.
/// <para>
/// This is a struct (but will be a ref struct in .Net 9): it must be short-lived and used locally.
/// </para>
/// </summary>
public struct FilteredTokenSpanEnumerator : IFilteredTokenSpanEnumerator
{
    FilteredTokenSpanEnumeratorImpl _impl;

    /// <summary>
    /// Initializes a new enumerator.
    /// </summary>
    /// <param name="matches">The filtered tokens.</param>
    /// <param name="tokens">The source code tokens.</param>
    public FilteredTokenSpanEnumerator( IReadOnlyList<FilteredTokenSpan> matches, IReadOnlyList<Token> tokens )
    {
        _impl = new( matches, tokens );
    }

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.IsEmpty"/>
    public bool IsEmpty => _impl.IsEmpty;

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.IsSingleEach"/>
    public bool IsSingleEach => _impl.IsSingleEach;

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.State"/>
    public FilteredTokenSpanEnumeratorState State => _impl.State;

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.CurrentMatch"/>
    public FilteredTokenSpan CurrentMatch => _impl.CurrentMatch;

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.Token"/>
    public SourceToken Token => _impl.Token;

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.NextEach"/>
    public bool NextEach() => _impl.NextEach();

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.NextMatch"/>
    public bool NextMatch() => _impl.NextMatch();

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.NextToken"/>
    public bool NextToken() => _impl.NextToken();
}


