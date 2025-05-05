using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    /// Parsing errors should be inlined <see cref="TokenError"/>. 
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
    /// Must parse the <paramref name="tokenSpec"/> and/or <paramref name="tokenPattern"/> (at least one is not null).
    /// and build a <see cref="IFilteredTokenEnumerableProvider"/> or an error string.
    /// </summary>
    /// <param name="tokenSpec">The pre-parsed token specification if any.</param>
    /// <param name="tokenPattern">The pre-parsed token pattern if any.</param>
    /// <returns>The provider or an error string.</returns>
    internal protected virtual object CreateFilteredTokenProvider( TransformerHost.Language language,
                                                                   RawString? tokenSpec,
                                                                   RawString? tokenPattern )
    {
        Throw.DebugAssert( tokenSpec != null || tokenPattern != null );
        Type? sType = null;
        if( tokenSpec != null )
        {
            var singleSpanType = tokenSpec.InnerText.Span.Trim();
            if( singleSpanType.Length > 0 )
            {
                sType = singleSpanType switch
                {
                    "statement" => typeof( TransformStatement ),
                    "in" => typeof( InScope ),
                    "replace" => typeof( ReplaceStatement ),
                    _ => null
                };
                if( sType == null )
                {
                    return $"""
                    Invalid span type '{singleSpanType}'. Allowed are "statement", "in", "replace".
                    """;
                }
            }
        }
        var s = tokenSpec != null ? ParseSpanSpec( language, tokenSpec ) : null;
        if( s is string spanError ) return spanError;
        Throw.CheckState( "ParseSpanSpec must return a IFilteredTokenEnumerableProvider or an error string",
                           s is null or IFilteredTokenEnumerableProvider );
        var spanSpec = s as IFilteredTokenEnumerableProvider;

        var p = tokenPattern != null ? ParsePattern( language, tokenPattern, spanSpec ) : null;
        if( p is string tokenError ) return tokenError;
        Throw.CheckState( "ParsePattern must return a IFilteredTokenEnumerableProvider or an error string",
                           p is null or IFilteredTokenEnumerableProvider );
        var pattern = p as IFilteredTokenEnumerableProvider;

        if( s == null && p == null )
        {
            return "No span specification nor pattern can be parsed.";
        }
        if( s == null ) return pattern!;
        if( p == null ) return spanSpec!;
        return IFilteredTokenEnumerableProvider.Combine( spanSpec, pattern! );
    }

    protected virtual object ParseSpanSpec( TransformerHost.Language language, RawString tokenSpec )
    {
        var singleSpanType = tokenSpec.InnerText.Span.Trim();
        if( singleSpanType.Length > 0 )
        {
            var sType = singleSpanType switch
            {
                "statement" => typeof( TransformStatement ),
                "in" => typeof( InScope ),
                "replace" => typeof( ReplaceStatement ),
                _ => null
            };
            if( sType == null )
            {
                return $"""
                    Invalid span type '{singleSpanType}'. Allowed are "statement", "in", "replace".
                    """;
            }
        }
    }

    protected virtual object ParsePattern( TransformerHost.Language language, RawString tokenPattern, IFilteredTokenEnumerableProvider spanSpec )
    {
        var head = new TokenizerHead( tokenPattern.InnerText, language.TargetAnalyzer );
        ParseStandardMatchPattern( ref head );
        if( head.FirstParseError != null )
        {
            return head.FirstParseError.ErrorMessage;
        }
        Throw.DebugAssert( !head.IsCondemned );
        if( head.Tokens.Count == 0 )
        {
            return "No token found in match pattern.";
        }
        return new TokenSpanFilter( head.Tokens.ToImmutableArray() );
    }

    protected abstract void ParseStandardMatchPattern( ref TokenizerHead head );
}
