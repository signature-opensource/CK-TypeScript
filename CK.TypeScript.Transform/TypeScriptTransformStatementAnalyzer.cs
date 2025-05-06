using CK.Transform.Core;
using System;

namespace CK.TypeScript.Transform;

sealed class TypeScriptTransformStatementAnalyzer : TransformStatementAnalyzer, ILowLevelTokenizer
{
    readonly TypeScriptAnalyzer _tsAnalyzer;

    internal TypeScriptTransformStatementAnalyzer( TransformLanguage language, TypeScriptAnalyzer tsAnalyzer )
        : base( language )
    {
        _tsAnalyzer = tsAnalyzer;
    }

    public LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        // No specific tokens needed currently.
        return TransformLanguage.MinimalTransformerLowLevelTokenize( head );
    }

    protected override TransformStatement? ParseStatement( TransformerHost.Language language, ref TokenizerHead head )
    {
        int begStatement = head.LastTokenIndex + 1;
        if( head.TryAcceptToken( "ensure", out _ ) )
        {
            var subHead = head.CreateSubHead( out var safetyToken, _tsAnalyzer );
            var importToken = subHead.MatchToken( "import" );
            if( importToken is not TokenError )
            {
                var importStatement = ImportStatement.Match( ref subHead, importToken );
                head.SkipTo( safetyToken, ref subHead );
                return importStatement != null
                        ? head.AddSpan( new EnsureImportStatement( begStatement, head.LastTokenIndex + 1 ) )
                        : null;
            }
        }
        return base.ParseStatement( language, ref head );
    }

    protected override object ParseSpanSpec( TransformerHost.Language language, RawString tokenSpec )
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
        return IFilteredTokenEnumerableProvider.Empty;
    }
}
