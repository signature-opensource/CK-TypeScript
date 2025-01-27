using CK.Transform.Core;

namespace CK.Less.Transform;

public class LessAnalyzer : Tokenizer, IAnalyzer
{
    public string LanguageName => LessLanguage._languageName;

    protected override void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptCLikeLineComment();
        c.AcceptCLikeStarComment();
    }

    protected override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        var t = LowLevelToken.GetBasicTokenType( head );
        if( t.TokenType is TokenType.SingleQuote or TokenType.DoubleQuote )
        {
            t = LowLevelToken.BasicallyReadQuotedString( head );
        }
        return t;
    }


    protected override TokenError? Tokenize( ref TokenizerHead head )
    {
        throw new NotImplementedException();
    }

    public AnalyzerResult Parse( ReadOnlyMemory<char> text )
    {
        Reset( text );
        return Parse();
    }
}
