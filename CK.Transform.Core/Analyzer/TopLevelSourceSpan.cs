namespace CK.Transform.Core;

/// <summary>
/// A top-level <see cref="SourceSpan"/> supports the <see cref="ITopLevelAnalyzer{T}"/>
/// (but can be used independently).
/// </summary>
public abstract class TopLevelSourceSpan : SourceSpan
{
    /// <summary>
    /// Initializes a new TopLevelSourceSpan.
    /// </summary>
    /// <param name="beg">The start of the span. Must be greater or equal to 0.</param>
    /// <param name="end">The end of the span. Must be greater than <paramref name="beg"/>.</param>
    protected TopLevelSourceSpan( int beg, int end )
        : base( beg, end )
    {
    }

    /// <summary>
    /// Gets or sets this top-level element name.
    /// This is optional: some language can have anonymous top-level element.
    /// <para>
    /// This can be initially set by the parsing but can be changed.
    /// </para>
    /// </summary>
    public abstract string? Name { get; set; }
}

