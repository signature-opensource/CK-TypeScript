using System.Collections.Generic;
using System;

namespace CK.Transform.Core;

public sealed partial class SpanMatcher : IFilteredTokenOperator
{
    /// <summary>
    /// Activates the "pattern" and then the {span specification} if they are not null.
    /// </summary>
    /// <param name="collector">The provider collector.</param>
    public void Activate( Action<IFilteredTokenOperator> collector )
    {
        _pattern?.Activate( collector );
        _spanSpec?.Activate( collector );
    }

    void IFilteredTokenOperator.Apply( IFilteredTokenOperatorContext context, IReadOnlyList<FilteredTokenSpan> input )
    {
        IFilteredTokenOperator.ThrowOnCombinedOperator();
    }
}
