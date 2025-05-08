using CK.Transform.Core;
using System;

namespace CK.Less.Transform;

sealed class LessTransformStatementAnalyzer : LanguageTransformAnalyzer
{
    readonly LessAnalyzer _lessAnalyzer;

    internal LessTransformStatementAnalyzer( TransformerHost.Language language, LessAnalyzer lessAnalyzer )
        : base( language, lessAnalyzer )
    {
        _lessAnalyzer = lessAnalyzer;
    }

    protected override TransformStatement? ParseStatement( ref TokenizerHead head )
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
        return base.ParseStatement( ref head );
    }

}
