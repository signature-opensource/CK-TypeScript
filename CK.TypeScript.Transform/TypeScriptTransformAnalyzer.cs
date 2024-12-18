using CK.Transform.Core;
using CK.Transform.TransformLanguage;
using System;

namespace CK.TypeScript.Transform;

sealed class TypeScriptTransformAnalyzer : BaseTransformAnalyzer
{
    readonly TransformerHost _host;

    internal TypeScriptTransformAnalyzer( TransformerHost host, TypeScriptLanguage language )
        : base( host, language )
    {
        _host = host;
    }

    public override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        var c = head[0];
        if( c == '{' ) return new LowLevelToken( NodeType.OpenBrace, 1 );
        if( c == '}' ) return new LowLevelToken( NodeType.CloseBrace, 1 );
        return base.LowLevelTokenize( head );
    }

    protected override IAbstractNode? ParseStatement( ref ParserHead head )
    {
        if( head.TryMatchToken( "ensure", out var ensure ) )
        {
            var import = head.MatchToken( "import" );
            if( import is TokenErrorNode ) return import;

            // Type only import. There should be a default import or named imports but not both.
            // Here we accept both.
            head.TryMatchToken( "type", out var typeOnly );
            TokenNode? defaultImportComma = null;
            // If there is a default import, there can be a namespace definition after.
            // The following is valid but redundant:
            //      import DefImport1, * as DefImport2 from '@angular/core';
            //
            // But after a namespace declaration, no default import is allowed. This is invalid:
            //      import * as DefImport2, DefImport1 from '@angular/core';
            //
            if( head.TryMatchToken( NodeType.GenericIdentifier, out var defaultImport ) )
            {
                head.TryMatchToken( NodeType.Comma, out defaultImportComma );               
            }

            TokenNode? asT = null;
            TokenNode? asNamespace = null;
            if( head.TryMatchToken(NodeType.Asterisk, out var asterisk ) )
            {
                asT = head.MatchToken( "as" );
                if( asT is TokenErrorNode ) return asT;
                asNamespace = head.MatchToken( NodeType.GenericIdentifier, "namespace" );
                if( asNamespace is TokenErrorNode ) return asNamespace;
            }
            else if( head.TryMatchToken( NodeType.OpenBrace, out var openBrace ) )
            {

            }
            

        }
        return base.ParseStatement( ref head );
    }

}
