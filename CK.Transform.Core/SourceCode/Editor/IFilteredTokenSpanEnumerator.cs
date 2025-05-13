using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

/// <summary>
/// Helper to ease working with list of <see cref="FilteredTokenSpan"/>.
/// </summary>
public interface IFilteredTokenSpanEnumerator
{
    /// <summary>
    /// Gets the current <see cref="FilteredTokenSpan"/>.
    /// <para>
    /// State must be  <see cref="FilteredTokenSpanEnumeratorState.Match"/> or <see cref="FilteredTokenSpanEnumeratorState.Token"/>
    /// or an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    FilteredTokenSpan CurrentMatch { get; }

    /// <summary>
    /// Gets whether there is no token at all to enumerate.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Gets whether there's at least one token in a single "each" bucket.
    /// </summary>
    bool IsSingleEach { get; }

    /// <summary>
    /// Gets the input of this enumerator.
    /// </summary>
    IReadOnlyList<FilteredTokenSpan> Input { get; }

    /// <summary>
    /// Gets the current input index, regardless of the State.
    /// This starts at 0 and ends at Input's length.
    /// </summary>
    int CurrentInputIndex { get; }

    /// <summary>
    /// Gets the current "each" bucket number.
    /// <para>
    /// State must not be <see cref="FilteredTokenSpanEnumeratorState.Finished"/> oherwise
    /// an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    int CurrentEachIndex { get; }

    /// <summary>
    /// Gets this enumerator state.
    /// </summary>
    FilteredTokenSpanEnumeratorState State { get; }

    /// <summary>
    /// Gets the current token and its position in the source code.
    /// <para>
    /// State must be <see cref="FilteredTokenSpanEnumeratorState.Token"/> oherwise
    /// an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    SourceToken Token { get; }

    /// <summary>
    /// Moves to the next "each" bucket.
    /// This initializes this enumerator when State is <see cref="FilteredTokenSpanEnumeratorState.Unitialized"/>.
    /// </summary>
    /// <returns>
    /// True if move succeeded, false if the <see cref="FilteredTokenSpanEnumeratorState.Finished"/> state
    /// has been reached.
    /// </returns>
    bool NextEach();

    /// <summary>
    /// Moves to the next match in the current each bucket.
    /// <para>
    /// <c>NextEach()</c> must have been called before oherwise
    /// an <see cref="InvalidOperationException"/> is thrown.
    /// This is necessarily true after a successful call to <c>NextEach()</c>.
    /// </para>
    /// </summary>
    /// <returns>
    /// True if move succeeded, false if the <see cref="FilteredTokenSpanEnumeratorState.Finished"/> state
    /// has been reached or there's no more match in the current "each" bucket and <c>NextEach()</c> must be called.
    /// </returns>
    bool NextMatch();

    /// <summary>
    /// Gets the first, last and count of tokens of the current match and
    /// moves to the next match in the current each bucket.
    /// <para>
    /// <c>NextEach()</c> must have been called before oherwise
    /// an <see cref="InvalidOperationException"/> is thrown.
    /// This is necessarily true after a successful call to <c>NextEach()</c>.
    /// </para>
    /// </summary>
    /// <param name="currentFirst">The first token in the current match.</param>
    /// <param name="currentLast">The last token in the current match.</param>
    /// <param name="currentCount">The number of tokens in the current match.</param>
    /// <returns>
    /// True if move succeeded, false if the <see cref="FilteredTokenSpanEnumeratorState.Finished"/> state
    /// has been reached or there's no more match in the current "each" bucket and <c>NextEach()</c> must be called.
    /// </returns>
    bool NextMatch( out SourceToken currentFirst, out SourceToken currentLast, out int currentCount );

    /// <summary>
    /// Moves to the next token in the current match.
    /// <para>
    /// <c>NextMatch()</c> must have been called before oherwise
    /// an <see cref="InvalidOperationException"/> is thrown.
    /// This is necessarily true after a successful call to <c>NextMatch()</c>.
    /// </para>
    /// </summary>
    /// <returns>
    /// True if move succeeded, false if the <see cref="FilteredTokenSpanEnumeratorState.Finished"/> state
    /// has been reached or there's no more match in the current match and <c>NextMatch()</c> must be called.
    /// </returns>
    bool NextToken();
}
