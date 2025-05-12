using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;

namespace CK.TypeScript.Transform;

/// <summary>
/// TypeScript language anlayzer.
/// </summary>
public sealed partial class TypeScriptAnalyzer : TargetLanguageAnalyzer
{
    readonly Scanner _scanner;

    /// <summary>
    /// Initialize a new TypeScriptAnalyzer.
    /// </summary>
    public TypeScriptAnalyzer()
        : base( TypeScriptLanguage._languageName )
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


    /// <summary>
    /// Overridden to handle "{braces}", "{class}", "{import}".
    /// </summary>
    /// <param name="tokenSpec">The span specification to analyze.</param>
    /// <returns>The provider or an error string.</returns>
    protected override object ParseSpanSpec( RawString tokenSpec )
    {
        var singleSpanType = tokenSpec.InnerText.Span.Trim();
        if( singleSpanType.Length > 0 )
        {
            return singleSpanType switch
            {
                "braces" => new SingleSpanTypeFilter( typeof( BraceSpan ), "{braces}" ),
                "class" => new SingleSpanTypeFilter( typeof( ClassDefinition ), "{class}" ),
                "import" => new SingleSpanTypeFilter( typeof( ImportStatement ), "{import}" ),
                _ => $"""
                     Invalid span type '{singleSpanType}'. Allowed are "braces", "class", "import".
                     """
            };
        }
        return IFilteredTokenOperator.Empty;
    }

    protected override void ParseStandardMatchPattern( ref TokenizerHead head )
    {
        _scanner.Reset();
        _scanner.TokenOnlyParse( ref head );
    }
}
