using System.Collections.Generic;
using System;

namespace CK.Transform.Core;

public sealed partial class SpanMatcher : IFilteredTokenEnumerableProvider
{
    Func<ITokenFilterBuilderContext,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> IFilteredTokenEnumerableProvider.GetFilteredTokenProjection()
    {
        return IFilteredTokenEnumerableProvider.ThrowOnCombinedProvider();
    }

    /// <summary>
    /// Activates the "pattern" and then the {span specification} if they are not null.
    /// </summary>
    /// <param name="collector">The provider collector.</param>
    public void Activate( Action<IFilteredTokenEnumerableProvider> collector )
    {
        _pattern?.Activate( collector );
        _spanSpec?.Activate( collector );
    }



}
