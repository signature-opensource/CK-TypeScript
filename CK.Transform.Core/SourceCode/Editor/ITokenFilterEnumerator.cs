using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

/// <summary>
/// Enumerates matches in a <see cref="TokenFilter"/> or in the <see cref="IScopedCodeEditor.Tokens"/>.
/// </summary>
public interface ITokenFilterEnumerator
{
    /// <summary>
    /// Gets the current <see cref="TokenMatch"/>.
    /// <para>
    /// State must be  <see cref="TokenFilterEnumeratorState.Match"/> or <see cref="TokenFilterEnumeratorState.Token"/>
    /// or an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    TokenMatch CurrentMatch { get; }

    /// <summary>
    /// Gets whether there's a single "each" bucket.
    /// </summary>
    bool IsSingleEach { get; }

    /// <summary>
    /// Gets the input of this enumerator.
    /// </summary>
    IReadOnlyList<TokenMatch> Input { get; }

    /// <summary>
    /// Gets the current input index, regardless of the State.
    /// This starts at 0 and ends at Input's length.
    /// </summary>
    int CurrentInputIndex { get; }

    /// <summary>
    /// Gets the current "each" bucket number.
    /// <para>
    /// State must not be <see cref="TokenFilterEnumeratorState.Finished"/> oherwise
    /// an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    int CurrentEachIndex { get; }

    /// <summary>
    /// Gets this enumerator state.
    /// </summary>
    TokenFilterEnumeratorState State { get; }

    /// <summary>
    /// Gets the current token and its position in the source code.
    /// <para>
    /// State must be <see cref="TokenFilterEnumeratorState.Token"/> oherwise
    /// an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    SourceToken Token { get; }

    /// <summary>
    /// Moves to the next "each" bucket.
    /// This initializes this enumerator when State is <see cref="TokenFilterEnumeratorState.Unitialized"/>.
    /// </summary>
    /// <returns>
    /// True if move succeeded, false if the <see cref="TokenFilterEnumeratorState.Finished"/> state
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
    /// True if move succeeded, false if the <see cref="TokenFilterEnumeratorState.Finished"/> state
    /// has been reached or there's no more match in the current "each" bucket and <c>NextEach()</c> must be called.
    /// </returns>
    bool NextMatch();

    /// <summary>
    /// Moves to the next match in the current each bucket and on success
    /// reads the first, last and count tokens of the match. This enumerator
    /// is positionned on the <paramref name="last"/> token.
    /// <para>
    /// <c>NextEach()</c> must have been called before oherwise
    /// an <see cref="InvalidOperationException"/> is thrown.
    /// This is necessarily true after a successful call to <c>NextEach()</c>.
    /// </para>
    /// </summary>
    /// <param name="first">On sucess, the first token of the match, false <see cref="SourceToken.IsValid"/> otherwise.</param>
    /// <param name="last">On sucess, the last token of the match, false <see cref="SourceToken.IsValid"/> otherwise.</param>
    /// <param name="count">On sucess, the number of tokens in the match, 0 otherwise.</param>
    /// <returns>
    /// True if move succeeded, false if the <see cref="TokenFilterEnumeratorState.Finished"/> state
    /// has been reached or there's no more match in the current "each" bucket and <c>NextEach()</c> must be called.
    /// </returns>
    bool NextMatch( out SourceToken first, out SourceToken last, out int count );

    /// <summary>
    /// Moves to the next token in the current match.
    /// <para>
    /// <c>NextMatch()</c> must have been called before oherwise
    /// an <see cref="InvalidOperationException"/> is thrown.
    /// This is necessarily true after a successful call to <c>NextMatch()</c>.
    /// </para>
    /// </summary>
    /// <returns>
    /// True if move succeeded, false if the <see cref="TokenFilterEnumeratorState.Finished"/> state
    /// has been reached or there's no more match in the current match and <c>NextMatch()</c> must be called.
    /// </returns>
    bool NextToken();
}
