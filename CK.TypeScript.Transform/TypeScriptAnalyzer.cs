using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;

namespace CK.TypeScript.Transform;

/// <summary>
/// TypeScript language anlayzer.
/// </summary>
public sealed partial class TypeScriptAnalyzer : Analyzer, ITargetAnalyzer
{
    readonly Scanner _scanner;

    /// <summary>
    /// Initialize a new TypeScriptAnalyzer.
    /// </summary>
    public TypeScriptAnalyzer()
    {
        _scanner = new Scanner();
    }

    /// <summary>
    /// Calls <see cref="TriviaHeadExtensions.AcceptCLikeLineComment(ref TriviaHead)"/>
    /// and <see cref="TriviaHeadExtensions.AcceptCLikeStarComment(ref TriviaHead)"/>.
    /// </summary>
    /// <param name="c">The trivia head.</param>
    protected override void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptCLikeLineComment();
        c.AcceptCLikeStarComment();
    }

    /// <inheritdoc/>
    public override AnalyzerResult Parse( ReadOnlyMemory<char> text )
    {
        _scanner.Reset();
        return base.Parse( text );
    }

    /// <inheritdoc/>
    protected override void DoParse( ref TokenizerHead head )
    {
        for(; ; )
        {
            var t = _scanner.GetNextToken( ref head );
            if( t.TokenType is TokenType.EndOfInput )
            {
                return;
            }
            // Handles import statement (but not import(...) functions) only here (top-level constructs).
            if( t.TextEquals( "import" ) && head.LowLevelTokenType is not TokenType.OpenParen )
            {
                ImportStatement.Match( ref head, t );
            }
            if( t is not TokenError )
            {
                _scanner.HandleKnownSpan( ref head, t );
            }
        }
    }

}
