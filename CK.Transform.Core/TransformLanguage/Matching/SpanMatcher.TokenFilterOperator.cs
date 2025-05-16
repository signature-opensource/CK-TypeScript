using System;

namespace CK.Transform.Core;

public sealed partial class SpanMatcher : ITokenFilterOperator
{
    /// <summary>
    /// Activates the "pattern" and then the {span specification} if they are not null.
    /// </summary>
    /// <param name="collector">The provider collector.</param>
    public void Activate( Action<ITokenFilterOperator> collector )
    {
        _pattern?.Activate( collector );
        _spanSpec?.Activate( collector );
    }

    void ITokenFilterOperator.Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        ITokenFilterOperator.ThrowOnCombinedOperator();
    }
}
