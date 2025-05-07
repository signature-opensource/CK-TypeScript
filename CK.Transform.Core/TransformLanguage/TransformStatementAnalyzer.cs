using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CK.Transform.Core;

/// <summary>
/// Base class for transform language analyzer: this parses <see cref="TransformStatement"/>.
/// <para>
/// Specializations can implement <see cref="ILowLevelTokenizer"/> if the transform language
/// requires more than the default low level tokens handled by <see cref="TransformerHost.RootTransformAnalyzer.LowLevelTokenize(ReadOnlySpan{char})"/>
/// (that are <see cref="TransformLanguage.MinimalTransformerLowLevelTokenize(ReadOnlySpan{char})"/>).
/// </para>
/// </summary>
public abstract class TransformStatementAnalyzer
{
    readonly TransformLanguage _language;

    /// <summary>
    /// Initializes a new analyzer.
    /// </summary>
    /// <param name="language">The transform language.</param>
    protected TransformStatementAnalyzer( TransformLanguage language )
    {
        _language = language;
    }

    /// <summary>
    /// Gets the <see cref="TransformLanguage"/>.
    /// </summary>
    public TransformLanguage Language => _language;

    /// <summary>
    /// Must implement transform specific statement parsing.
    /// Parsing errors are inlined <see cref="TokenError"/>. 
    /// <para>
    /// At this level, this handles transform statements that apply to any language:
    /// <see cref="ReparseStatement"/>, <see cref="InjectIntoStatement"/>,
    /// <see cref="InScopeStatement"/>, <see cref="ReplaceStatement"/> and
    /// <see cref="TransformStatementBlock"/> (<c>begin</c>...<c>end</c> blocks).
    /// </para>
    /// </summary>
    /// <param name="language">The host's language.</param>
    /// <param name="head">The head.</param>
    /// <returns>The parsed statement or null.</returns>
    internal protected virtual TransformStatement? ParseStatement( TransformerHost.Language language, ref TokenizerHead head )
    {
        if( head.TryAcceptToken( "inject", out var inject ) )
        {
            return InjectIntoStatement.Parse( ref head, inject );
        }
        if( head.TryAcceptToken( "in", out var inT ) )
        {
            return InScopeStatement.Parse( language, ref head, inT );
        }
        if( head.TryAcceptToken( "replace", out var replaceT ) )
        {
            return ReplaceStatement.Parse( language, ref head, replaceT );
        }
        if( head.TryAcceptToken( "reparse", out _ ) )
        {
            int begStatement = head.LastTokenIndex;
            head.TryAcceptToken( TokenType.SemiColon, out _ );
            return head.AddSpan( new ReparseStatement( begStatement, head.LastTokenIndex + 1 ) );
        }
        if( head.LowLevelTokenText.Equals( "begin", StringComparison.Ordinal ) )
        {
            return TransformStatementBlock.Parse( language, ref head );
        }
        return null;
    }

    /// <summary>
    /// Must parse the <paramref name="tokenSpec"/> build a <see cref="IFilteredTokenEnumerableProvider"/>
    /// or an error string.
    /// </summary>
    /// <param name="language">The current language.</param>
    /// <param name="tokenSpec">The pre-parsed token specification if any.</param>
    /// <returns>The provider or an error string.</returns>
    internal protected virtual object ParseSpanSpec( TransformerHost.Language language, RawString tokenSpec )
    {
        var content = tokenSpec.InnerText.Span.Trim();
        if( content.Length > 0 )
        {
            return $"""
                Invalid span spectification '{content}'. Language {language.LanguageName} does't handle any span specification.
                """;
        }
        return IFilteredTokenEnumerableProvider.EmptyProjection;
    }


    /// <summary>
    /// Must parse the <paramref name="tokenPattern"/> build a <see cref="IFilteredTokenEnumerableProvider"/>
    /// or an error string.
    /// <para>
    /// At this level, the target language analyzer is used to create a head on the <see cref="RawString.InnerText"/>,
    /// <see cref="ParseStandardMatchPattern"/> is called and a <see cref="TokenSpanFilter"/> is created on the
    /// parsed tokens (or an error string if no tokens have been parsed).
    /// </para>
    /// </summary>
    /// <param name="language">The current language.</param>
    /// <param name="tokenPattern">The pre-parsed token pattern if any.</param>
    /// <param name="spanSpec">
    /// Optional associated {span specification} that appears before in a <see cref="SpanMatcher"/>.
    /// For some languages the span specification can contain hint for the pattern parsing and/or matching.
    /// </param>
    /// <returns>The provider or an error string.</returns>
    internal protected virtual object ParsePattern( TransformerHost.Language language, RawString tokenPattern, IFilteredTokenEnumerableProvider? spanSpec )
    {
        var head = new TokenizerHead( tokenPattern.InnerText, language.TargetAnalyzer );
        ParseStandardMatchPattern( language, ref head );
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
    /// calls this on a <paramref name="head"/> that uses the <see cref="TransformerHost.Language.TargetAnalyzer"/>
    /// as the <see cref="ITokenizerHeadBehavior"/>.
    /// <para>
    /// At this level, this simply <see cref="TokenizerHead.AcceptLowLevelToken(TokenType)"/> on all tokens
    /// until <see cref="TokenizerHead.EndOfInput"/> is reached and emits an error on unknown token (when
    /// <see cref="TokenizerHead.LowLevelTokenType"/> is <see cref="TokenType.None"/>).
    /// </para>
    /// <para>
    /// If the target language uses a <see cref="ITokenScanner"/>, parsing must be defferred to it: complex tokens
    /// will be parsed instead of only the ones obtained from the low-level tokens.
    /// </para>
    /// </summary>
    /// <param name="language">The current language.</param>
    /// <param name="head">The head on the pattern to analyze, bound to the <see cref="ITargetAnalyzer"/>.</param>
    protected virtual void ParseStandardMatchPattern( TransformerHost.Language language, ref TokenizerHead head )
    {
        while( head.EndOfInput == null )
        {
            if( head.LowLevelTokenType == TokenType.None )
            {
                head.AppendError( $"Unknown token '{head.Head[0]}'.", 1 );
                break;
            }
            else
            {
                head.AcceptLowLevelToken();
            }
        }
    }
}
