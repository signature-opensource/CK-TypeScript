namespace CK.Transform.Core;

/// <summary>
/// Optional concept that an <see cref="Analyzer"/> can use: a scanner is
/// usually a stateful object that, thanks to its internal state, is able to
/// handle more complex tokens than the <see cref="TokenizerHead"/>.
/// <para>
/// Such scanner is required to parse javascript inline regular expressions
/// (<c>/d+w*/i</c>), interpolated strings (to handles the code islands in them)
/// or to maintain a current brace depth for instance.
/// </para>
/// </summary>
public interface ITokenScanner
{
    /// <summary>
    /// Returns the next token.
    /// </summary>
    /// <param name="text">The tokenizer head.</param>
    /// <returns>The token (can be an error or the <see cref="TokenizerHead.EndOfInput"/> token).</returns>
    Token GetNextToken( ref TokenizerHead head );
}
