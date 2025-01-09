using CK.Core;
using CK.Transform.Core;

namespace CK.Transform.Core;

/// <summary>
/// Base class for all transform statement.
/// </summary>
public abstract class TransformStatement : SourceSpan
{
    /// <summary>
    /// Initializes a new transform statement that is a SourceSpan.
    /// </summary>
    /// <param name="beg">The start of the span. Must be greater or equal to 0.</param>
    /// <param name="end">The end of the span. Must be greater than <paramref name="beg"/>.</param>
    protected TransformStatement( int beg, int end )
        : base( beg, end )
    {
    }

    /// <summary>
    /// Must apply the transformation. 
    /// </summary>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="editor">The code to transform.</param>
    /// <returns>True on success, false on error. Any error must be logged.</returns>
    public abstract bool Apply( IActivityMonitor monitor, SourceCodeEditor editor );
}
