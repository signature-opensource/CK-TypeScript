using System.Collections;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CK.Transform.Core;

/// <summary>
/// Provides filtering token capabilities.
/// </summary>
public interface ITokenFilter
{
    /// <summary>
    /// Computes scoped tokens from <see cref="ScopedTokensBuilder.Tokens"/>.
    /// </summary>
    /// <param name="builder">The builder to use.</param>
    /// <returns>The result. Must be ignored when <see cref="ScopedTokensBuilder.HasError"/> is true.</returns>
    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> GetScopedTokens( ScopedTokensBuilder builder );
}
