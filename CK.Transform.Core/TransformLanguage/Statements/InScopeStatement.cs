using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CK.Transform.Core;

/// <summary>
/// Captures:
/// <code>
/// in ...
///  in ...
///    begin
///     ...
///    end
/// </code>
/// </summary>
public sealed class InScopeStatement : TransformStatement
{
    InScopeStatement( int beg, int end )
        : base( beg, end )
    {
    }

    /// <summary>
    /// Checks that there is at least one <see cref="Scopes"/> and a non null <see cref="Body"/>.
    /// </summary>
    /// <returns>True if this span is valid.</returns>
    [MemberNotNullWhen( true, nameof( Body ) )]
    public override bool CheckValid()
    {
        return base.CheckValid()
               && Children.FirstChild is InScope
               && Body is not null;
    }

    /// <summary>
    /// Gets the "in ..." clauses.
    /// Non empty when <see cref="CheckValid()"/> is true.
    /// </summary>
    public IEnumerable<InScope> Scopes => Children.OfType<InScope>();

    /// <summary>
    /// Gets the statements.
    /// Never null when <see cref="CheckValid()"/> is true.
    /// </summary>
    public TransformStatementBlock? Body => Children.LastChild as TransformStatementBlock;

    public override void Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        throw new NotImplementedException();
    }

    internal static InScopeStatement? Parse( TransformerHost.Language language, ref TokenizerHead head, Token inToken )
    {
        Throw.DebugAssert( inToken.Text.Span.Equals( "in", StringComparison.Ordinal ) );
        int begSpan = head.LastTokenIndex + 1;
        while( InScope.Match( language, ref head ) != null ) ;
        var body = TransformStatementBlock.Parse( language, ref head );
        return body == null
                ? null
                : head.AddSpan( new InScopeStatement( begSpan, head.LastTokenIndex + 1 ) ); 
    }
}
