namespace CK.Transform.Core;

/// <summary>
/// Optional extension for <see cref="ITokenFilterOperator"/> to handle <see cref="SpanMatcher"/> where "Pattern".
/// </summary>
public interface ITokenFilterAnchoredOperator
{
    /// <summary>
    /// Creates a filter operator that knows that its previous operator
    /// is a "Pattern" from a <see cref="SpanMatcher"/> where "Pattern".
    /// </summary>
    /// <returns>A token filter operator.</returns>
    ITokenFilterOperator ToAnchoredOperator();
}
