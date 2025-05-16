using System.Collections.Generic;

namespace CK.Transform.Core;


/// <summary>
/// Read only view of the <see cref="SourceCode.Spans"/>.
/// </summary>
public interface ISourceSpanRoot : IEnumerable<SourceSpan>
{
    /// <summary>
    /// Gets the deepest span at a given position.
    /// <para>
    /// <see cref="SourceSpan.Parent"/> can be used to retrieve all the parent covering spans.
    /// </para>
    /// </summary>
    /// <param name="index">The token index.</param>
    /// <returns>The deepest span or null if no span covers the token at this position.</returns>
    SourceSpan? GetChildrenSpanAt( int index );
}
