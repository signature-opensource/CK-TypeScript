namespace CK.Transform.Core;

/// <summary>
/// Captures the relationship between two <see cref="TokenSpan"/>.
/// There are 7 oredered cases. By swapping the 2 spans, 13 different relationships exist (Equals is not swapped). 
/// </summary>
public enum SpanRelationship
{
    /// <summary>
    /// The two spans are equals.
    /// <see cref="TokenSpan.Empty"/> is equal to itself. The <see cref="Swapped"/> bit is never set in this case.
    /// </summary>
    Equal,

    /// <summary>
    /// The first span starts before the second one. Both have the same <see cref="TokenSpan.End"/>.
    /// </summary>
    SameEnd,

    /// <summary>
    /// Both spans have the same <see cref="TokenSpan.Beg"/>, the first one ends before the second one.
    /// </summary>
    SameStart,

    /// <summary>
    /// The first span ends where the second one begins.
    /// </summary>
    Contiguous,

    /// <summary>
    /// The first span is before the second one and doesn't intersect it.
    /// The first span can be the <see cref="Empty"/>.
    /// </summary>
    Independent,

    /// <summary>
    /// The first span starts before the second one and ends inside the second one.
    /// </summary>
    Overlapped,

    /// <summary>
    /// The first span contains the second one (it starts before and ends after the second one).
    /// </summary>
    Contained,

    /// <summary>
    /// When this bit is set, the first and second span roles are reversed.
    /// </summary>
    Swapped = 32
}
