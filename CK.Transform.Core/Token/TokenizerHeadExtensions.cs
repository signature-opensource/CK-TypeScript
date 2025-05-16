using CK.Core;

namespace CK.Transform.Core;

/// <summary>
/// Extends TokenizerHead.
/// </summary>
public static class TokenizerHeadExtensions
{
    /// <summary>
    /// Adds a span. This throws if the span is already attached to a root or
    /// if it intersects an already added existing span.
    /// </summary>
    /// <param name="newOne">The span to add.</param>
    /// <returns>The <paramref name="newOne"/> span.</returns>
    public static T AddSpan<T>( this ref TokenizerHead head, T newOne ) where T : SourceSpan
    {
#if DEBUG
        var s = head.AddSourceSpan( newOne );
        Throw.DebugAssert( s.CheckValid() );
        return (T)s;
#else
        return Unsafe.As<T>( head.AddSourceSpan( newOne ) );
#endif
    }

}
