using CK.Transform.Core;

namespace CK.Transform.TransformLanguage;

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
}
