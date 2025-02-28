namespace CK.Transform.Core;

/// <summary>
/// A top-level <see cref="SourceSpan"/> supports the <see cref="ITopLevelAnalyzer{T}"/>
/// (but can be used independently).
/// </summary>
public abstract class TopLevelSourceSpan : SourceSpan
{
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

