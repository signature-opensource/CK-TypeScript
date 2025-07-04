namespace CK.Transform.Core;

/// <summary>
/// A micro parser for trivias is a function that analyses the <see cref="TriviaHead.Head"/>
/// and calls <see cref="TriviaHead.Accept(TokenType, int)"/>, <see cref="TriviaHead.EndOfInput(TokenType)"/>
/// or does nothing because it doesn't recognize the pattern.
/// <para>
/// These micro parser can easily be combined, see <see cref="TriviaHead.ParseAll(TriviaParser)"/> and
/// <see cref="TriviaHead.ParseAny(System.Collections.Immutable.ImmutableArray{TriviaParser})"/>
/// </para>
/// <para>
/// The <see cref="TriviaHeadExtensions"/> defines several standard micro parser.
/// </para>
/// </summary>
/// <param name="head">The parser head.</param>
public delegate void TriviaParser( ref TriviaHead head );
