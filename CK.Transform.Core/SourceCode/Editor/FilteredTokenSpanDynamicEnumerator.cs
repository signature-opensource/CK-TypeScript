using CK.Core;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CK.Transform.Core;

sealed class FilteredTokenSpanDynamicEnumerator : IFilteredTokenSpanEnumerator
{
    FilteredTokenSpanEnumeratorImpl _impl;

    public FilteredTokenSpanDynamicEnumerator()
    {
    }

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.IsEmpty"/>
    public bool IsEmpty => _impl.IsEmpty;

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.IsSingleEach"/>
    public bool IsSingleEach => _impl.IsSingleEach;

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.Input"/>
    public IReadOnlyList<FilteredTokenSpan> Input => _impl.Input;

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.CurrentInputIndex"/>
    public int CurrentInputIndex => _impl.CurrentInputIndex;

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.CurrentEachIndex"/>
    public int CurrentEachIndex => _impl.CurrentEachIndex;

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

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.NextMatch"/>
    public bool NextMatch( out SourceToken currentFirst, out SourceToken currentLast, out int currentCount )
        => _impl.NextMatch( out currentFirst, out currentLast, out currentCount );

    /// <inheritdoc cref="IFilteredTokenSpanEnumerator.NextToken"/>
    public bool NextToken() => _impl.NextToken();

    internal void Reset( IReadOnlyList<FilteredTokenSpan> matches, IReadOnlyList<Token> tokens )
    {
        _impl.Reset( matches, tokens );
    }

    internal void OnUpdateTokens( int eLimit, int delta )
    {
        if( _impl.State is not FilteredTokenSpanEnumeratorState.Finished )
        {
            _impl.OnUpdateTokens( eLimit, delta );
        }
    }

}
