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
        var c = head[0];
        if( c == '{' ) return new LowLevelToken( TokenType.OpenBrace, 1 );
        if( c == '}' ) return new LowLevelToken( TokenType.CloseBrace, 1 );
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

    //protected override IAbstractNode? ParseStatement( ref ParserHead head )
    //{
    //    if( head.TryMatchToken( "ensure", out var ensure ) )
    //    {
    //        var import = head.MatchToken( "import" );
    //        if( import is TokenErrorNode ) return import;

    //        // Type only import. There should be a default import or named imports but not both.
    //        // Here we accept both.
    //        head.TryMatchToken( "type", out var typeOnly );
    //        TokenNode? defaultImportComma = null;
    //        // If there is a default import, there can be a namespace definition after.
    //        // The following is valid but redundant:
    //        //      import DefImport1, * as DefImport2 from '@angular/core';
    //        //
    //        // But after a namespace declaration, no default import is allowed. This is invalid:
    //        //      import * as DefImport2, DefImport1 from '@angular/core';
    //        //
    //        if( head.TryMatchToken( TokenType.GenericIdentifier, out var defaultImport ) )
    //        {
    //            head.TryMatchToken( TokenType.Comma, out defaultImportComma );               
    //        }

    //        TokenNode? asT = null;
    //        TokenNode? asNamespace = null;
    //        if( head.TryMatchToken(TokenType.Asterisk, out var asterisk ) )
    //        {
    //            asT = head.MatchToken( "as" );
    //            if( asT is TokenErrorNode ) return asT;
    //            asNamespace = head.MatchToken( TokenType.GenericIdentifier, "namespace" );
    //            if( asNamespace is TokenErrorNode ) return asNamespace;
    //        }
    //        else if( head.TryMatchToken( TokenType.OpenBrace, out var openBrace ) )
    //        {

    //        }
            

    //    }
    //    return base.ParseStatement( ref head );
    //}

}
