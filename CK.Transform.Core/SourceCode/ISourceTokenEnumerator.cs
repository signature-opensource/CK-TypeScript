using System;

namespace CK.Transform.Core;

/// <summary>
/// Special enumerator of <see cref="SourceCode.Tokens"/>.
/// This must be disposed once done with it.
/// </summary>
public interface ISourceTokenEnumerator : IDisposable
{
    /// <summary>
    /// Gets the token index.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the token.
    /// </summary>
    public Token Token { get; }

    /// <summary>
    /// Gets the deepest span that covers the <see cref="Token"/>.
    /// <para>
    /// <see cref="SourceSpan.Parent"/> can be used to retrieve all the parent covering spans.
    /// </para>
    /// </summary>
    public SourceSpan? Span { get; }

    /// <summary>
    /// Advances to the next token.
    /// </summary>
    /// <returns></returns>
    public bool MoveNext();
}


