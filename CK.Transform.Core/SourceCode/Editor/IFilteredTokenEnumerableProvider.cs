using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

/// <summary>
/// A filtered token provider is able to provide a projection for a each/range/token
/// enumerable.
/// <para>
/// A <c>IEnumerable&lt;IEnumerable&lt;IEnumerable&lt;SourceToken&gt;&gt;&gt;</c> captures
/// each/range/token structure: tokens are grouped by ranges and ranges
/// are grouped by each buckets. See <see cref="LocationCardinality.LocationKind.Each"/>.
/// </para>
/// </summary>
public interface IFilteredTokenEnumerableProvider
{
    /// <summary>
    /// Empty singleton for <c>IEnumerable&lt;IEnumerable&lt;IEnumerable&lt;SourceToken&gt;&gt;&gt;</c>.
    /// </summary>
    public static readonly IEnumerable<IEnumerable<IEnumerable<SourceToken>>> EmptyFilteredTokens = [[[]]];

    /// <summary>
    /// No-op projection that <see cref="GetFilteredTokenProjection"/> can use.
    /// </summary>
    public static readonly Func<IActivityMonitor,
                                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
                                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> EmptyProjection = ( m, e ) => e;

    /// <summary>
    /// Provides a function that projects a <c>IEnumerable&lt;IEnumerable&lt;IEnumerable&lt;SourceToken&gt;&gt;&gt;</c>.
    /// <para>
    /// The function is free to use lazy evaluation (and should do so whenever possible): the monitor provided to the
    /// function can be captured by closure and used to signal errors.
    /// </para>
    /// <para>
    /// Implementations should use <see cref="EmptyFilteredTokens"/> when no projection must be returned.
    /// </para>
    /// </summary>
    /// <returns>The projection.</returns>
    Func<IActivityMonitor,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> GetFilteredTokenProjection(); 
}
