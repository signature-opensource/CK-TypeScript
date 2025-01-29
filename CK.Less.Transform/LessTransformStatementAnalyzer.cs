using CK.Transform.Core;

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

    protected override TransformStatement? ParseStatement( ref TokenizerHead head )
    {
        int begStatement = head.LastTokenIndex;
        if( head.TryAcceptToken( "ensure", out var _ ) )
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
        return base.ParseStatement( ref head );
    }

}
