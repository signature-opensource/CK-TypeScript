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
    /// Must analyze the <paramref name="tokenSpec"/> and return a <see cref="IFilteredTokenEnumerableProvider"/>
    /// or an error string.
    /// <para>
    /// At this level, this returns the "Invalid span specification '...'. Language '...' doesn't handle any span specification."
    /// error or the <see cref="IFilteredTokenEnumerableProvider.Empty"/> if the span specification is empty (to allow <c>in {} "..."</c>
    /// syntax).
    /// </para>
    /// </summary>
    /// <param name="tokenSpec">The pre-parsed token specification.</param>
    /// <returns>The provider or an error string.</returns>
    internal protected virtual object ParseSpanSpec( RawString tokenSpec )
    {
        var content = tokenSpec.InnerText.Span.Trim();
        if( content.Length > 0 )
        {
            return $"""
                Invalid span specification '{content}'. Language '{_languageName}' doesn't handle any span specification.
                """;
        }
        return IFilteredTokenEnumerableProvider.Empty;
    }

    /// <summary>
    /// Must parse the <paramref name="tokenPattern"/> and return a <see cref="IFilteredTokenEnumerableProvider"/>
    /// or an error string.
    /// <para>
    /// By default, at this level, a head is created on the <see cref="RawString.InnerText"/>,
    /// <see cref="ParseStandardMatchPattern"/> is called and a <see cref="TokenSpanFilter"/> is created on the
    /// parsed tokens (or an error string if no tokens have been parsed).
    /// </para>
    /// </summary>
    /// <param name="language">The current language.</param>
    /// <param name="tokenPattern">The pre-parsed token pattern.</param>
    /// <param name="spanSpec">
    /// Optional associated {span specification} that appears before the pattern in a <see cref="SpanMatcher"/>.
    /// For some languages the span specification can contain hints for the pattern parsing and/or matching.
    /// </param>
    /// <returns>The provider or an error string.</returns>
    internal protected virtual object ParsePattern( RawString tokenPattern,
                                                    IFilteredTokenEnumerableProvider? spanSpec )
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
        return new TokenSpanFilter( head.Tokens.ToImmutableArray() );
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

}
