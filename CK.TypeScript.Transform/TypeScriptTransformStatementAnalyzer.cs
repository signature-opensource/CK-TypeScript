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

    protected override TransformStatement? ParseStatement( ref TokenizerHead head )
    {
        int begStatement = head.LastTokenIndex;
        if( head.TryAcceptToken( "ensure", out var _ ) )
        {
            var subHead = head.CreateSubHead( out var safetyToken, _tsAnalyzer );
            var importToken = subHead.MatchToken( "import" );
            if( importToken is not TokenError )
            {
                var importStatement = ImportStatement.TryMatch( importToken, ref subHead );
                head.SkipTo( safetyToken, ref subHead );
                return importStatement != null
                        ? new EnsureImportStatement( begStatement, head.LastTokenIndex + 1, importStatement )
                        : null;
            }
        }
        return base.ParseStatement( ref head );
    }

}
