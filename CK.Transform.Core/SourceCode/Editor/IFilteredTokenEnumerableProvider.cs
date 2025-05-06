using CK.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

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
    public static readonly Func<TokenFilterBuilderContext,
                                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
                                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> EmptyProjection = ( m, e ) => e;

    sealed class EmptyProvider : IFilteredTokenEnumerableProvider
    {
        public Func<TokenFilterBuilderContext,
                    IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
                    IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> GetFilteredTokenProjection()
        {
            return EmptyProjection;
        }
    }

    /// <summary>
    /// Singleton empty provider that returns a <see cref="EmptyProjection"/>
    /// </summary>
    public static readonly IFilteredTokenEnumerableProvider Empty = new EmptyProvider();


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
    Func<TokenFilterBuilderContext,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> GetFilteredTokenProjection();


    /// <summary>
    /// Combines two provivers into one, first applying the <paramref name="inner"/> and then <paramref name="outer"/>.
    /// </summary>
    /// <param name="outer">The last provider to consider (can be null or <see cref="Empty"/>).</param>
    /// <param name="inner">The first provider to consider (can be null or <see cref="Empty"/>).</param>
    /// <returns>A combined provider or <paramref name="outer"/>, <paramref name="inner"/> or <see cref="Empty"/>.</returns>
    public static IFilteredTokenEnumerableProvider Combine( IFilteredTokenEnumerableProvider? outer, IFilteredTokenEnumerableProvider? inner )
    {
        if( outer == null || outer == Empty ) return inner ?? Empty;
        if( inner == null || inner == Empty ) return outer;
        return new Combined( outer, inner );
    }

    sealed record class Combined( IFilteredTokenEnumerableProvider outer, IFilteredTokenEnumerableProvider inner ) : IFilteredTokenEnumerableProvider
    {
        public Func<TokenFilterBuilderContext,
                    IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
                    IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> GetFilteredTokenProjection() => Combine;

        IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Combine( TokenFilterBuilderContext c,
                                                                    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> input )
        {
            return outer.GetFilteredTokenProjection()( c, inner.GetFilteredTokenProjection()( c, input ) );
        }
    }


}
