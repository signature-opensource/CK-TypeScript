using CK.Core;

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
    /// Applies the transformation.
    /// Errors are emitted to the <paramref name="monitor"/> (that is the <see cref="SourceCodeEditor.Monitor"/>)
    /// to fail a transformation: <see cref="SourceCodeEditor.HasError"/> is the final result.
    /// </summary>
    /// <param name="monitor">The <see cref="SourceCodeEditor.Monitor"/>.</param>
    /// <param name="editor">The code to transform.</param>
    public abstract void Apply( IActivityMonitor monitor, SourceCodeEditor editor );
}
