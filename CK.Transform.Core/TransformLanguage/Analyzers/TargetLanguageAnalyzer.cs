using CK.Core;
using System;
using System.Collections.Immutable;

namespace CK.Transform.Core;

/// <summary>
/// A target language analyzer parses target language texts and provides support for its <see cref="TransformLanguageAnalyzer"/>
/// by handling the <see cref="SpanMatcher"/> analysis.
/// </summary>
public abstract class TargetLanguageAnalyzer : Analyzer, ITargetAnalyzer
{
    readonly string _languageName;

    /// <summary>
    /// Initializes a new analyzer.
    /// </summary>
    /// <param name="languageName">The language name.</param>
    /// <param name="handleWhiteSpaceTrivias">Initial and default <see cref="ILowLevelTokenizer.HandleWhiteSpaceTrivias"/> value.</param>
    protected TargetLanguageAnalyzer( string languageName, bool handleWhiteSpaceTrivias = true )
        : base( handleWhiteSpaceTrivias )
    {
        _languageName = languageName;
    }

    /// <summary>
    /// Gets the language name.
    /// </summary>
    public string LanguageName => _languageName;

    /// <summary>
    /// Must analyze the <paramref name="tokenSpec"/> and return a <see cref="ITokenFilterOperator"/>
    /// or an error string.
    /// <para>
    /// At this level, this returns the "Invalid span specification '...'. Language '...' doesn't handle any span specification."
    /// error, even if the span specification is empty to disallow <c>in {} where "..."</c>.
    /// syntax).
    /// </para>
    /// </summary>
    /// <param name="tokenSpec">The pre-parsed token specification.</param>
    /// <returns>The provider or an error string.</returns>
    internal protected virtual object ParseSpanSpec( BalancedString tokenSpec )
    {
        return $"""
                Invalid span specification '{tokenSpec.Text}'. Language '{_languageName}' doesn't handle any span specification.
                """;
    }

    /// <summary>
    /// Must parse the <paramref name="tokenPattern"/> and return a <see cref="ITokenFilterOperator"/>
    /// or an error string.
    /// <para>
    /// By default, at this level, a head is created on the <see cref="RawString.InnerText"/>,
    /// <see cref="ParseStandardMatchPattern"/> is called and a <see cref="TokenPatternOperator"/> is created on the
    /// parsed tokens (or an error string if no tokens have been parsed).
    /// </para>
    /// </summary>
    /// <param name="tokenPattern">The pre-parsed token pattern.</param>
    /// <param name="spanSpec">
    /// The optional {span specification} that appears before the pattern in a <see cref="SpanMatcher"/>
    /// when where "Pattern" syntax is used.
    /// For some languages the span specification can contain hints for the pattern parsing and/or matching.
    /// At this level, this drives whether the returned <see cref="TokenPatternOperator"/> will be in "where" mode.
    /// </param>
    /// <returns>The provider or an error string.</returns>
    internal protected virtual object ParsePattern( RawString tokenPattern,
                                                    ITokenFilterOperator? spanSpec )
    {
        var head = new TokenizerHead( tokenPattern.InnerText, this );
        ParseStandardMatchPattern( ref head );
        if( head.FirstError != null )
        {
            return head.FirstError.ErrorMessage;
        }
        Throw.DebugAssert( !head.IsCondemned );
        if( head.Tokens.Count == 0 )
        {
            return "No token found in match pattern.";
        }
        return new TokenPatternOperator( tokenPattern, head.Tokens.ToImmutableArray(), whereMode: spanSpec != null );
    }

    /// <summary>
    /// Extension point: the default implementation of <see cref="ParsePattern"/>
    /// calls this on a <paramref name="head"/> (bound to the <see cref="ITokenizerHeadBehavior"/>).
    /// <para>
    /// At this level, this simply calls <see cref="TokenizerHead.AcceptLowLevelTokenOrNone()"/> 
    /// until an error or <see cref="TokenizerHead.EndOfInput"/> is reached.
    /// </para>
    /// <para>
    /// If the target language uses a <see cref="ITokenScanner"/>, parsing must be deferred to it: complex tokens
    /// will be parsed instead of only the ones obtained from the low-level tokens.
    /// </para>
    /// </summary>
    /// <param name="head">The head on the pattern to analyze.</param>
    protected virtual void ParseStandardMatchPattern( ref TokenizerHead head )
    {
        while( head.EndOfInput == null )
        {
            if( head.AcceptLowLevelTokenOrNone() is TokenError )
            {
                break;
            }
        }
    }

    /// <summary>
    /// Must create a <see cref="Trivia"/> for an <see cref="InjectionPoint"/>.
    /// </summary>
    /// <param name="target">The injection point.</param>
    /// <param name="syntax">The <see cref="InjectionPoint.Kind"/> to create.</param>
    /// <param name="inlineIfPossible">
    /// True to avoid an ending new line if possible.
    /// A block comment can be chosen rather than a line comment.
    /// </param>
    /// <returns>The trivia.</returns>
    protected internal abstract Trivia CreateInjectionPointTrivia( InjectionPoint target,
                                                                   InjectionPoint.Kind syntax = InjectionPoint.Kind.AutoClosing,
                                                                   bool inlineIfPossible = false );

    /*
            var tt = TokenTypeExtensions.GetTriviaBlockCommentType( 4, 3 );
            var marker = new Trivia( tt, $"<!-- <{target.Name} /> -->{Environment.NewLine}" );
     */

}
