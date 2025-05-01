using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

/// <summary>
/// Alias on <c>IEnumerable&lt;IEnumerable&lt;IEnumerable&lt;SourceToken&gt;&gt;&gt;</c>.
/// This captures each/range/token structure: tokens are grouped by ranges and ranges
/// are grouped by each buckets. See <see cref="LocationCardinality.LocationKind.Each"/>.
/// </summary>
public interface IScopedTokenEnumerable : IEnumerable<IEnumerable<IEnumerable<SourceToken>>>
{
    /// <summary>
    /// Empty singleton.
    /// </summary>
    public static readonly IScopedTokenEnumerable Empty = (IScopedTokenEnumerable)(IEnumerable<IEnumerable<IEnumerable<SourceToken>>>)[[[]]];

    /// <summary>
    /// No-op projection that <see cref="IScopedTokenEnumerableProvider"/> can use.
    /// </summary>
    public static readonly Func<IActivityMonitor, IScopedTokenEnumerable, IScopedTokenEnumerable> EmptyProjection = ( m, e ) => e;
}
