using CK.Core;
using System.Collections.Generic;

namespace CK.Transform.Core;

public interface ITokenFilter
{
    IEnumerable<IEnumerable<IEnumerable<SourceToken>>>? GetScopedTokens( IActivityMonitor monitor, SourceCodeEditor editor );
}
