using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

public sealed class TransformStatementBlock : TransformStatement
{
    public TransformStatementBlock( int beg, int end, List<TransformStatement> statements )
        : base( beg, end )
    {
        Statements = statements;
    }

    public IReadOnlyList<TransformStatement> Statements { get; }

    public override bool Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        bool success = !editor.NeedReparse || editor.Reparse( monitor );
        if( success )
        {
            foreach( var statement in Statements )
            {
                success &= statement.Apply( monitor, editor );
            }
            if( success )
            {
                editor.ApplyChanges();
            }
        }
        return success;
    }
}
