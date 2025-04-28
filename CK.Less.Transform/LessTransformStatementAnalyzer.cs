using CK.Transform.Core;
using System;

namespace CK.Less.Transform;

sealed class LessTransformStatementAnalyzer : TransformStatementAnalyzer, ILowLevelTokenizer
{
    readonly LessAnalyzer _lessAnalyzer;

    internal LessTransformStatementAnalyzer( TransformLanguage language, LessAnalyzer lessAnalyzer )
        : base( language )
    {
        _lessAnalyzer = lessAnalyzer;
    }

    public LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        // No specific tokens needed currently.
        return TransformLanguage.MinimalTransformerLowLevelTokenize( head );
    }

    protected override TransformStatement? ParseStatement( TransformerHost.Language language, ref TokenizerHead head )
    {
        int begStatement = head.LastTokenIndex;
        if( head.TryAcceptToken( "ensure", out _ ) )
        {
            var subHead = head.CreateSubHead( out var safetyToken, _lessAnalyzer );
            var importToken = subHead.MatchToken( "@import" );
            if( importToken is not TokenError )
            {
                var importStatement = EnsureImportStatement.TryMatch( begStatement, ref subHead );
                head.SkipTo( safetyToken, ref subHead );
                return importStatement;
            }
        }
        return base.ParseStatement( language, ref head );
    }

}
