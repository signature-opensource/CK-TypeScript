using System;

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
    /// </para>
    /// </summary>
    /// <returns>
    /// True if move succeeded, false if the <see cref="FilteredTokenSpanEnumeratorState.Finished"/> state
    /// has been reached or there's no more match in the current "each" bucket and <c>NextEach()</c> must be called.
    /// </returns>
    bool NextMatch();

    /// <summary>
    /// Moves to the next token in the current match.
    /// <para>
    /// <c>NextMatch()</c> must have been called before oherwise
    /// an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <returns>
    /// True if move succeeded, false if the <see cref="FilteredTokenSpanEnumeratorState.Finished"/> state
    /// has been reached or there's no more match in the current match and <c>NextMatch()</c> must be called.
    /// </returns>
    bool NextToken();
}
