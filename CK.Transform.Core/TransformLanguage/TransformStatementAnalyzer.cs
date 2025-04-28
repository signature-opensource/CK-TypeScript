using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Transform.Core;


/// <summary>
/// Base class for transform language analyzer: this parses <see cref="TransformStatement"/>.
/// <para>
/// Specializations can implement <see cref="ILowLevelTokenizer"/> if the transform language
/// requires more than the default low level tokens handled by <see cref="TransformerHost.RootTransformAnalyzer.LowLevelTokenize(ReadOnlySpan{char})"/>.
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
    /// <see cref="ReparseStatement"/>, <see cref="InjectIntoStatement"/> and
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
}
