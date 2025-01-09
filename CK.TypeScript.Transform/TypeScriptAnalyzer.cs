using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace CK.TypeScript.Transform;

sealed partial class TypeScriptAnalyzer : Tokenizer, IAnalyzer
{
    public string LanguageName => TypeScriptLanguage._languageName;

    public TypeScriptAnalyzer()
    {
        _interpolated = new Stack<int>();
    }

    protected override void Reset( ReadOnlyMemory<char> text )
    {
        _braceDepth = 0;
        _interpolated.Clear();
        base.Reset( text );
    }

    protected override void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptCLikeLineComment();
        c.AcceptCLikeStarComment();
    }

    protected override TokenError? Tokenize( ref TokenizerHead head )
    {
        for(; ; )
        {
            var t = Scan( ref head );
            if( t is TokenError e )
            {
                return e.TokenType == TokenType.EndOfInput ? null : e;
            }
        }
    }

    public AnalyzerResult Parse( ReadOnlyMemory<char> text )
    {
        Reset( text );
        return Parse();
    }
}
