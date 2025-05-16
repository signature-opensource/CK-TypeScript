using System;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// Operator that transforms a <see cref="TokenFilter"/>.
/// </summary>
public interface ITokenFilterOperator
{
    /// <summary>
    /// Applies the operation.
    /// This is never called if this operator is a combined one. See <see cref="Activate"/>:
    /// implementations should use the <see cref="ThrowOnCombinedOperator"/> helper.
    /// </summary>
    /// <param name="context">The operator context.</param>
    /// <param name="source">The source to consider.</param>
    /// <returns>The transfomed matches.</returns>
    void Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource source );

    /// <summary>
    /// Activates this operator by collecting itself or collecting subordinate operators if this operator
    /// is a combined one. Not collecting any operator is not an error, and there is no reason for
    /// this method to fail.
    /// </summary>
    /// <param name="collector">The collector.</param>
    void Activate( Action<ITokenFilterOperator> collector );

    /// <summary>
    /// Writes a description of the operator: usually the source code
    /// that defines this operator.
    /// </summary>
    /// <param name="b">The builder.</param>
    /// <param name="parsable">
    /// True to obtain a parsable string if possible.
    /// False contains type decorations. 
    /// </param>
    /// <returns>The string builder.</returns>
    StringBuilder Describe( StringBuilder b, bool parsable );

    /// <summary>
    /// Typically implemented by calling <see cref="Describe(StringBuilder, bool)"/> with parsable
    /// set to true.
    /// </summary>
    /// <returns>A readable string.</returns>
    string ToString();

    /// <summary>
    /// Singleton empty provider. It activates no operator and doesn't change the filter.
    /// </summary>
    public static readonly ITokenFilterOperator Empty = new EmptyOperator();

    private sealed class EmptyOperator : ITokenFilterOperator
    {
        public void Activate( Action<ITokenFilterOperator> collector )
        {
        }

        public void Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
        {
            context.SetUnchangedResult();
        }

        public StringBuilder Describe( StringBuilder b, bool parsable ) => b.Append( "(empty)" );

        public override string ToString() => "(empty)";

    }

    /// <summary>
    /// Centralized helper for <see cref="Apply"/> implementations of combined operators.
    /// </summary>
    /// <exception cref="NotSupportedException">Always throws a NotSupportedException.</exception>
    /// <returns>Never returns. Here to enable a simple use with return.</returns>
    public static TokenMatch[] ThrowOnCombinedOperator()
    {
        throw new NotSupportedException( "Never called as this is a combined operator." );
    }

}
