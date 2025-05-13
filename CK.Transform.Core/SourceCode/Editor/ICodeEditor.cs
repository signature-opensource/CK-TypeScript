using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

public interface ICodeEditor : IDisposable
{
    /// <summary>
    /// Gets the source code tokens.
    /// </summary>
    IReadOnlyList<Token> UnfilteredTokens { get; }

    /// <summary>
    /// Replaces the tokens starting at <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index of the first token that must be replaced.</param>
    /// <param name="tokens">Updated tokens. Must not be empty.</param>
    void Replace( int index, params ReadOnlySpan<Token> tokens );

    /// <summary>
    /// Replaces one or more tokens with any number of tokens.
    /// </summary>
    /// <param name="index">The index of the first token that must be replaced.</param>
    /// <param name="count">The number of tokens to replace. Must be positive.</param>
    /// <param name="tokens">New tokens to insert. Must not be empty.</param>
    void Replace( int index, int count, params ReadOnlySpan<Token> tokens );

    /// <summary>
    /// Inserts new tokens. Spans that start at <paramref name="index"/> will contain the inserted tokens.
    /// </summary>
    /// <param name="index">The index of the inserted tokens.</param>
    /// <param name="tokens">New tokens to insert. Must not be empty.</param>
    void InsertAt( int index, params ReadOnlySpan<Token> tokens );

    /// <summary>
    /// Inserts new tokens. Spans that start at <paramref name="index"/> will not contain the inserted tokens.
    /// </summary>
    /// <param name="index">The index of the inserted tokens.</param>
    /// <param name="tokens">New tokens to insert. Must not be empty.</param>
    void InsertBefore( int index, params ReadOnlySpan<Token> tokens );

    /// <summary>
    /// Removes a token at a specified index.
    /// </summary>
    /// <param name="index">The token index to remove.</param>
    void RemoveAt( int index );

    /// <summary>
    /// Removes a range of tokens.
    /// </summary>
    /// <param name="index">The index of the first token to remove.</param>
    /// <param name="count">The number of tokens to remove. Must be positive.</param>
    void RemoveRange( int index, int count );

}
