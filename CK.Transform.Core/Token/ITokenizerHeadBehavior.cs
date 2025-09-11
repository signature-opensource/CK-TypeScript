namespace CK.Transform.Core;

/// <summary>
/// Low level <see cref="TokenizerHead"/> behavior handles the trivias and
/// provides the <see cref="TokenizerHead.LowLevelTokenType"/> and <see cref="TokenizerHead.LowLevelTokenText"/>.
/// </summary>
public interface ITokenizerHeadBehavior : ILowLevelTokenizer
{
    /// <summary>
    /// The default <see cref="TriviaParser"/> to apply.
    /// </summary>
    /// <param name="c">The trivia collector.</param>
    void ParseTrivia( ref TriviaHead c );
}

