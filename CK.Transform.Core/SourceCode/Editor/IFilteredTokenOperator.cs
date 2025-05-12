using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// A <see cref="FilteredTokenSpan"/> operator applies a transformation to an list of <see cref="FilteredTokenSpan"/>.
/// </summary>
public interface IFilteredTokenOperator
{
    /// <summary>
    /// Applies the operation.
    /// This is never called if this operator is a combined one. See <see cref="Activate"/>:
    /// implementations should use the <see cref="ThrowOnCombinedOperator"/> helper.
    /// </summary>
    /// <param name="context">The filter context.</param>
    /// <param name="input">The input to transform.</param>
    /// <returns>The transfomed matches.</returns>
    void Apply( IFilteredTokenOperatorContext context, IReadOnlyList<FilteredTokenSpan> input );

    /// <summary>
    /// Activates this operator by collecting itself or collecting subordinate operators if this operator
    /// is a combined one. Not collecting any operator is not an error, and there is no reason for
    /// this method to fail.
    /// </summary>
    /// <param name="collector">The collector.</param>
    void Activate( Action<IFilteredTokenOperator> collector );

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
    /// Centralized helper for <see cref="Apply"/> implementations of combined operators.
    /// </summary>
    /// <exception cref="NotSupportedException">Always throws a NotSupportedException.</exception>
    /// <returns>Never returns. Here to enable a simple use with return.</returns>
    public static FilteredTokenSpan[] ThrowOnCombinedOperator()
    {
        throw new NotSupportedException( "Never called as this is a combined operator." );
    }

}
