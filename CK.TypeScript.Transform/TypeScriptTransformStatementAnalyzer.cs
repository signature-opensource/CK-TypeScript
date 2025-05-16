using CK.Transform.Core;

namespace CK.TypeScript.Transform;

sealed class TypeScriptTransformStatementAnalyzer : TransformLanguageAnalyzer, ILowLevelTokenizer
{
    internal TypeScriptTransformStatementAnalyzer( TransformerHost.Language language, TypeScriptAnalyzer tsAnalyzer )
        : base( language, tsAnalyzer )
    {
    }

    protected override TransformStatement? ParseStatement( ref TokenizerHead head )
    {
        int begStatement = head.LastTokenIndex + 1;
        if( head.TryAcceptToken( "ensure", out _ ) )
        {
            var subHead = head.CreateSubHead( out var safetyToken, TargetAnalyzer );
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
        return base.ParseStatement( ref head );
    }

}
