using System;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Describes the <see cref="IVisitContext.RangeFilterStatus" />.
/// </summary>
[Flags]
public enum VisitedNodeRangeFilterStatus
{
    /// <summary>
    /// Currently visited node does not intersect the filter.
    /// </summary>
    None = 0,

    /// <summary>
    /// Currently visited node intersects the filter.
    /// When this bit is the only one set, it means that the visited node exactly covers
    /// the filtered range.
    /// </summary>
    FIntersecting = 1,

    /// <summary>
    /// Currently visited node starts before the filtered range.
    /// </summary>
    FBegBefore = 2,

    /// <summary>
    /// Currently visited node starts after the filtered range.
    /// </summary>
    FBegAfter = 4,

    /// <summary>
    /// Currently visited node ends before the filtered range.
    /// </summary>
    FEndBefore = 8,

    /// <summary>
    /// Currently visited node ends after the filtered range.
    /// </summary>
    FEndAfter = 16
}

/// <summary>
/// Extends <see cref="VisitedNodeRangeFilterStatus"/> with (hopefully) easier to understand methods.
/// </summary>
public static class VisitedNodeRangeFilterStatusExtension
{
    /// <summary>
    /// Currently visited node starts before the current <see cref="IVisitContextBase.RangeFilter"/>.
    /// </summary>
    /// <param name="this">This filter status.</param>
    /// <returns>Whether the visited node starts before the filtered range.</returns>
    public static bool IsBegBefore( this VisitedNodeRangeFilterStatus @this ) => (@this & VisitedNodeRangeFilterStatus.FBegBefore) != 0;

    /// <summary>
    /// Currently visited node starts after the current <see cref="IVisitContextBase.RangeFilter"/>.
    /// </summary>
    /// <param name="this">This filter status.</param>
    /// <returns>Whether the visited node starts after the filtered range.</returns>
    public static bool IsBegAfter( this VisitedNodeRangeFilterStatus @this ) => (@this & VisitedNodeRangeFilterStatus.FBegAfter) != 0;

    /// <summary>
    /// Currently visited node is the start of the current <see cref="IVisitContextBase.RangeFilter"/>.
    /// </summary>
    /// <param name="this">This filter status.</param>
    /// <returns>Whether the visited node is the start of the filtered range.</returns>
    public static bool IsFilteredRangeBeg( this VisitedNodeRangeFilterStatus @this ) => (@this & (VisitedNodeRangeFilterStatus.FBegAfter | VisitedNodeRangeFilterStatus.FBegBefore)) == 0;

    /// <summary>
    /// Currently visited node ends before the current <see cref="IVisitContextBase.RangeFilter"/>.
    /// </summary>
    /// <param name="this">This filter status.</param>
    /// <returns>Whether the visited node ends before the filtered range.</returns>
    public static bool IsEndBefore( this VisitedNodeRangeFilterStatus @this ) => (@this & VisitedNodeRangeFilterStatus.FEndBefore) != 0;

    /// <summary>
    /// Currently visited node ends after the current <see cref="IVisitContextBase.RangeFilter"/>.
    /// </summary>
    /// <param name="this">This filter status.</param>
    /// <returns>Whether the visited node ends after the filtered range.</returns>
    public static bool IsEndAfter( this VisitedNodeRangeFilterStatus @this ) => (@this & VisitedNodeRangeFilterStatus.FEndAfter) != 0;

    /// <summary>
    /// Currently visited node is the end of the current <see cref="IVisitContextBase.RangeFilter"/>.
    /// </summary>
    /// <param name="this">This filter status.</param>
    /// <returns>Whether the visited node is the end of the filtered range.</returns>
    public static bool IsFilteredRangeEnd( this VisitedNodeRangeFilterStatus @this ) => (@this & (VisitedNodeRangeFilterStatus.FEndAfter | VisitedNodeRangeFilterStatus.FEndBefore)) == 0;

    /// <summary>
    /// Currently visited node exactly covers the current <see cref="IVisitContextBase.RangeFilter"/>.
    /// </summary>
    /// <param name="this">This filter status.</param>
    /// <returns>Whether the visited node is exactly the filtered range.</returns>
    public static bool IsExactFilteredRange( this VisitedNodeRangeFilterStatus @this ) => @this == VisitedNodeRangeFilterStatus.FIntersecting;

    /// <summary>
    /// Currently visited node is contained in the current <see cref="IVisitContextBase.RangeFilter"/>.
    /// </summary>
    /// <param name="this">This filter status.</param>
    /// <returns>Whether the visited node is contained in the filtered range.</returns>
    public static bool IsIncludedInFilteredRange( this VisitedNodeRangeFilterStatus @this ) => !IsBegBefore( @this ) && !IsEndAfter( @this );
}
