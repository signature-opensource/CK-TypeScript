using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

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
    /// Provides a function that projects a <c>IEnumerable&lt;IEnumerable&lt;IEnumerable&lt;SourceToken&gt;&gt;&gt;</c>.
    /// <para>
    /// The function is free to use lazy evaluation (and should do so whenever possible): the monitor provided to the
    /// function can be captured by closure and used to signal errors.
    /// </para>
    /// <para>
    /// Implementations may use <see cref="EmptyFilteredTokens"/> when no projection must be returned but
    /// note that this method is called only on providers that have been collected by <see cref="Activate"/>.
    /// It should typically be implemented explicitely.
    /// </para>
    /// </summary>
    /// <returns>The projection.</returns>
    Func<ITokenFilterBuilderContext,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> GetFilteredTokenProjection();

    /// <summary>
    /// Activates this provider by collecting itself or collecting subordinate providers if this provider
    /// is a combined provider. Not collecting any provider is not an error, and there is no reason for
    /// this method to fail.
    /// <para>
    /// <see cref="Empty"/> is ignored and can be safely collected. 
    /// </para>
    /// </summary>
    /// <param name="collector">The collector.</param>
    void Activate( Action<IFilteredTokenEnumerableProvider> collector );

    /// <summary>
    /// Writes a description of the provider: usually the source code
    /// that defines this provider.
    /// </summary>
    /// <param name="b">The builder.</param>
    /// <param name="parsable">
    /// True to obtain a parsable string if possible.
    /// False contains type decorations. 
    /// </param>
    /// <returns></returns>
    StringBuilder Describe( StringBuilder b, bool parsable );

    /// <summary>
    /// Typically implemented by calling <see cref="Describe(StringBuilder, bool)"/> with parsable
    /// set to true.
    /// </summary>
    /// <returns>A readable string.</returns>
    string ToString();

    /// <summary>
    /// Empty singleton for <c>IEnumerable&lt;IEnumerable&lt;SourceToken&gt;&gt;</c>.
    /// </summary>
    public static readonly IEnumerable<IEnumerable<SourceToken>> EmptyRange = [[]];

    /// <summary>
    /// Empty singleton for <c>IEnumerable&lt;IEnumerable&lt;IEnumerable&lt;SourceToken&gt;&gt;&gt;</c>.
    /// </summary>
    public static readonly IEnumerable<IEnumerable<IEnumerable<SourceToken>>> EmptyFilteredTokens = [EmptyRange];

    /// <summary>
    /// No-op projection that <see cref="GetFilteredTokenProjection"/> can use.
    /// </summary>
    public static readonly Func<ITokenFilterBuilderContext,
                                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
                                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> EmptyProjection = ( m, e ) => e;

    /// <summary>
    /// Singleton empty provider. It activates no provider and returns a <see cref="EmptyProjection"/>
    /// </summary>
    public static readonly IFilteredTokenEnumerableProvider Empty = new EmptyProvider();

    private sealed class EmptyProvider : IFilteredTokenEnumerableProvider
    {
        public void Activate( Action<IFilteredTokenEnumerableProvider> collector )
        {
        }

        public Func<ITokenFilterBuilderContext,
                    IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
                    IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> GetFilteredTokenProjection()
        {
            return EmptyProjection;
        }

        public StringBuilder Describe( StringBuilder b, bool parsable ) => b.Append( "(empty)" );

        public override string ToString() => "(empty)";
    }

    /// <summary>
    /// Centralized helper for <see cref="GetFilteredTokenProjection"/> implementations of combined providers.
    /// </summary>
    /// <exception cref="NotSupportedException">Always throws a NotSupportedException.</exception>
    /// <returns>Never returns. Here to enable a simple use with return.</returns>
    public static Func<ITokenFilterBuilderContext,
                       IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
                       IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> ThrowOnCombinedProvider()
    {
        throw new NotSupportedException( "Never called as this is a combined provider." );
    }
}
