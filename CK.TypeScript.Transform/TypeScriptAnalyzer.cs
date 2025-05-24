using CK.Transform.Core;
using System;

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
    protected override object ParseSpanSpec( BalancedString tokenSpec )
    {
        var spanSpec = tokenSpec.InnerText.Trim();

        return spanSpec switch
        {
            "{}" or
            "braces" => new SpanEnclosedOperator( "{braces}", ( tokens, idx ) => tokens[idx].TokenType switch
                           {
                               TokenType.OpenBrace => EnclosingTokenType.Open,
                               TokenType.CloseBrace => EnclosingTokenType.Close,
                               _ => EnclosingTokenType.None
                           } ),
            "^{}" or
            "^braces" => new CoveringSpanEnclosedOperator( "{^braces}", ( tokens, idx ) => tokens[idx].TokenType switch
                            {
                                TokenType.OpenBrace => EnclosingTokenType.Open,
                                TokenType.CloseBrace => EnclosingTokenType.Close,
                                _ => EnclosingTokenType.None
                            } ),
            "[]" or
            "brackets" => new SpanEnclosedOperator( "{brackets}", ( tokens, idx ) => tokens[idx].TokenType switch
                            {
                                TokenType.OpenBracket => EnclosingTokenType.Open,
                                TokenType.CloseBracket => EnclosingTokenType.Close,
                                _ => EnclosingTokenType.None
                            } ),
            "^[]" or
            "^brackets" => new CoveringSpanEnclosedOperator( "{^brackets}", ( tokens, idx ) => tokens[idx].TokenType switch
                            {
                                TokenType.OpenBracket => EnclosingTokenType.Open,
                                TokenType.CloseBracket => EnclosingTokenType.Close,
                                _ => EnclosingTokenType.None
                            } ),
            "()" or
            "parens" => new SpanEnclosedOperator( "{parens}", ( tokens, idx ) => tokens[idx].TokenType switch
                            {
                                TokenType.OpenBracket => EnclosingTokenType.Open,
                                TokenType.CloseBracket => EnclosingTokenType.Close,
                                _ => EnclosingTokenType.None
                            } ),
            "^()" or
            "^parens" => new CoveringSpanEnclosedOperator( "{^parens}", ( tokens, idx ) => tokens[idx].TokenType switch
                            {
                                TokenType.OpenParen => EnclosingTokenType.Open,
                                TokenType.CloseParen => EnclosingTokenType.Close,
                                _ => EnclosingTokenType.None
                            } ),
            "class" => new SpanTypeOperator( "{class}", typeof( ClassDefinition ) ),
            "^class" => new CoveringSpanTypeOperator( "{class}", typeof( ClassDefinition ) ),
            "import" => new SpanTypeOperator( "{import}", typeof( ImportStatement ) ),
            _ => $$"""
                        Invalid span type '{spanSpec}'.
                        Allowed are "braces", "{}", "^braces", "^{}", "brackets", "[]", "^brackets", "^[]",
                        "class", "^class", "import".
                        """
        };
    }

    protected override void ParseStandardMatchPattern( ref TokenizerHead head )
    {
        _scanner.Reset();
        _scanner.TokenOnlyParse( ref head );
    }

    /// <inheritdoc/>
    protected override Trivia CreateInjectionPointTrivia( InjectionPoint target,
                                                          InjectionPoint.Kind syntax,
                                                          bool inlineIfPossible )
    {
        var tag = InjectionPoint.GetString( target.Name, syntax );
        return inlineIfPossible
                ? new Trivia( TokenTypeExtensions.GetTriviaBlockCommentType( 2, 2 ), $"/* {tag} */" )
                : new Trivia( TokenTypeExtensions.GetTriviaLineCommentType( 2 ), $"// {tag}{Environment.NewLine}" );
    }

}
