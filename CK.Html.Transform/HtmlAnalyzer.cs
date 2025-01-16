using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Html.Transform;



public sealed class HtmlAnalyzer : Tokenizer, IAnalyzer
{
    public string LanguageName => HtmlLanguage._languageName;

    public HtmlAnalyzer()
    {
    }

    /// <summary>
    /// Always false: in html, <see cref="HtmlTokenType.Text"/> contains whitespaces.
    /// </summary>
    public bool HandleWhiteSpaceTrivias => false;

    protected override void Reset( ReadOnlyMemory<char> text )
    {
        base.Reset( text );
    }

    protected override void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptXmlComment();
        c.AcceptXmlCDATA();
    }

    protected override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        int iS = 0;
        while( iS < head.Length && head[iS] != '<' ) iS++;
        if( iS > 0 ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS );

        return default;
        //if( head[0] == '<' )
        //{
        //    switch( _currentSpanType )
        //    {
        //        case SpanType.Text:
        //            return StartElementToken( head );

        //    }
        //}
    }

    public AnalyzerResult Parse( ReadOnlyMemory<char> text )
    {
        throw new NotImplementedException();
    }

    protected override TokenError? Tokenize( ref TokenizerHead head )
    {
        throw new NotImplementedException();
    }
}
