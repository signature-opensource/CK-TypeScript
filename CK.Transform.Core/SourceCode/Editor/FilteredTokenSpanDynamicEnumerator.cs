using System.Collections.Generic;

namespace CK.Transform.Core;

sealed class FilteredTokenSpanDynamicEnumerator : IFilteredTokenSpanEnumerator
{
    FilteredTokenSpanEnumeratorImpl _impl;

    public FilteredTokenSpanDynamicEnumerator()
    {
    }

    internal void Reset( IReadOnlyList<FilteredTokenSpan> matches, IReadOnlyList<Token> tokens )
    {
        _impl.Reset( matches, tokens );
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
