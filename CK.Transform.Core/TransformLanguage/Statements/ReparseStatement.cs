using CK.Core;

namespace CK.Transform.Core;

/// <summary>
/// Reparse the transformed source if needed: this is a no-operation when <see cref="SourceCodeEditor.SetNeedReparse()"/>
/// has not been called by previous transformations.
/// </summary>
public sealed class ReparseStatement : TransformStatement
{
    /// <summary>
    /// Initializes a new reparse statement.
    /// </summary>
    /// <param name="beg">The start of the span. Must be greater or equal to 0.</param>
    /// <param name="end">The end of the span. Must be greater than <paramref name="beg"/>.</param>
    public ReparseStatement( int beg, int end )
        : base( beg, end )
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Calls <see cref="SourceCodeEditor.Reparse(IActivityMonitor)"/> if and
    /// only if <see cref="SourceCodeEditor.NeedReparse"/> is true.
    /// </remarks>
    public override void Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        if( editor.NeedReparse )
        {
            editor.Reparse();
        }
    }
}
