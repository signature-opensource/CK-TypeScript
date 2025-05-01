using CK.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static CK.Transform.Core.TransformerHost;

namespace CK.Transform.Core;

/// <summary>
/// Statements block is the composite "begin ... end".
/// </summary>
public sealed class TransformStatementBlock : TransformStatement
{
    /// <summary>
    /// Initializes a new statements block.
    /// Parsed by <see cref="TransformStatementAnalyzer.ParseStatements(ref TokenizerHead)"/>.
    /// </summary>
    /// <param name="beg">The start of the span. Must be greater or equal to 0.</param>
    /// <param name="end">The end of the span. Must be greater than <paramref name="beg"/>.</param>
    TransformStatementBlock( int beg, int end )
        : base( beg, end )
    {
    }

    /// <summary>
    /// Gets the statements. Can be empty.
    /// </summary>
    public IEnumerable<TransformStatement> Statements => Children.OfType<TransformStatement>();

    /// <inheritdoc />
    /// <remarks>
    /// Applies the inner <see cref="Statements"/>.
    /// </remarks>
    public override void Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        foreach( var statement in Statements )
        {
            statement.Apply( monitor, editor );
            if( editor.HasError ) break;
        }
    }

    /// <summary>
    /// Parses a "begin ... end" block.
    /// Statements are parsed by <see cref="TransformStatementAnalyzer.ParseStatement(ref TokenizerHead)"/>.
    /// </summary>
    /// <param name="cLang">The language.</param>
    /// <param name="head">The head.</param>
    /// <returns>The list of transform statements.</returns>
    internal static TransformStatementBlock Parse( Language cLang, ref TokenizerHead head )
    {
        head.MatchToken( "begin" );
        int begSpan = head.LastTokenIndex;
        Token? foundEnd = null;
        while( head.EndOfInput == null && !head.TryAcceptToken( "end", out foundEnd ) )
        {
            var s = cLang.TransformStatementAnalyzer.ParseStatement( cLang, ref head );
            Throw.DebugAssert( s == null || s.CheckValid() );
            if( s == null )
            {
                head.AppendError( $"Failed to parse a transform '{cLang.LanguageName}' language statement.", -1 );
                break;
            }
        }
        if( foundEnd == null ) head.AppendError( "Expected 'end'.", 0 );
        return head.AddSpan( new TransformStatementBlock( begSpan, head.LastTokenIndex + 1 ) );
    }


}
