using CK.Core;
using CK.Transform.Core;
using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace CK.TypeScript.Transform;

public sealed class EnsureImportStatement : TransformStatement
{
    readonly ImportStatement _importStatement;

    internal EnsureImportStatement( int beg, int end, ImportStatement importStatement )
        : base( beg, end )
    {
        Throw.DebugAssert( "The span of the import has not been added.", importStatement.IsDetached );
        _importStatement = importStatement;
    }

    public override bool Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        // No need to respect any scope here. Imports are top-level statements.
        // Even if an import can appear anywhere in a file, it is not a good practice
        // and semantically useless.
        // No need to look for subordinated spans.
        var defined = editor.SourceCode.Spans.OfType<ImportStatement>();
        bool added = false;
        ImportStatement? lastImport = null;
        foreach( var import in defined )
        {

            lastImport = import;
        }
        if( !added )
        {
            var b = new StringBuilder();
            var importLine = _importStatement._line.Write( b, out var tokenCount );
            Token newText = new Token( TokenType.GenericAny, [], b.ToString().AsMemory(), Trivia.NewLine );
            int insertionPoint = lastImport?.Span.End ?? 0;
            editor.InsertBefore( insertionPoint, newText );
            editor.ApplyChanges();
            editor.SourceCode.Spans.Add( _importStatement, new TokenSpan( insertionPoint, insertionPoint + 1 ) );
        }
        return true;
    }
}
