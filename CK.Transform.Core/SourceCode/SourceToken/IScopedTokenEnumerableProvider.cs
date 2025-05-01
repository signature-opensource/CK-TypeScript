using CK.Core;
using System;

namespace CK.Transform.Core;

/// <summary>
/// A scoped token provider is able to provide a projection for a each/range/token
/// enumerable (see <see cref="IScopedTokenEnumerable"/>).
/// </summary>
public interface IScopedTokenEnumerableProvider
{
    /// <summary>
    /// Provides a function that projects a <see cref="IScopedTokenEnumerable"/>.
    /// <para>
    /// The function is free to use lazy evaluation (and should do so whenever possible): the monitor provided to the
    /// function can be captured by closure and used to signal errors.
    /// </para>
    /// <para>
    /// Implementations should use <see cref="IScopedTokenEnumerable.EmptyProjection"/> when no projection must be returned.
    /// </para>
    /// </summary>
    /// <returns>The projection.</returns>
    Func<IActivityMonitor, IScopedTokenEnumerable, IScopedTokenEnumerable> GetScopedTokenProjection(); 
}
