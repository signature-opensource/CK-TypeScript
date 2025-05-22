using System;

namespace CK.Transform.Core;

public sealed partial class SpanMatcher : ITokenFilterOperator
{
    /// <summary>
    /// Activates the {span specification} and then "pattern" if they are not null.
    /// </summary>
    /// <param name="collector">The provider collector.</param>
    public void Activate( Action<ITokenFilterOperator> collector )
    {
        _spanSpec?.Activate( collector );
        _pattern?.Activate( collector );
    }

    void ITokenFilterOperator.Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        ITokenFilterOperator.ThrowOnCombinedOperator();
    }
}
