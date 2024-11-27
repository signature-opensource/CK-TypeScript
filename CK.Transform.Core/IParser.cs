using System;

namespace CK.Transform.Core;

/// <summary>
/// Parser abstraction. This can be composed in a <see cref="CompositeParser"/>.
/// </summary>
public interface IParser
{
    /// <summary>
    /// Must parse a top-level language node and forward the <paramref name="text"/> accordingly.
    /// <para>
    /// When a parser doesn't recognize its language (typically because the very first token is unknwon), it
    /// must return a <see cref="TokenErrorNode.Unhandled"/>.
    /// If the parser recognizes its language, it returns either a successful <see cref="IAbstractNode"/> or
    /// a <see cref="TokenErrorNode"/> to signal an error.
    /// <para>
    /// Implementations must handle an empty <paramref name="text"/> (or a text composed only of trivias) by returning
    /// a <see cref="TokenErrorNode"/> with a <see cref="TokenType.EndOfInput"/>.
    /// </para>
    /// </para>
    /// <para>
    /// The notion of "top-level" is totally language dependent. A language can perfectly decide that a list of statements must be handled
    /// as a top-level node. However, it is recommended that such "aggregates" be managed by provided <see cref="AnalyzeOneOrMore"/> that supports
    /// the combination of multiple top-level nodes into a <see cref="NodeList{T}"/> of <see cref="AbstractNode"/>.
    /// </para>
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>A node (may be a <see cref="TokenErrorNode"/>).</returns>
    IAbstractNode Parse( ref ReadOnlyMemory<char> text );

    /// <summary>
    /// Must be overridden to return the language name (or names) handled by this parser.
    /// </summary>
    /// <returns>The language name.</returns>
    string ToString();
}
