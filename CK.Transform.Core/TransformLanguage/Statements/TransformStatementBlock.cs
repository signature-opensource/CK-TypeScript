using CK.Core;
using System.Collections.Generic;
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
    /// <param name="statements">Statements. Can be empty.</param>
    TransformStatementBlock( int beg, int end, List<TransformStatement> statements )
        : base( beg, end )
    {
        Statements = statements;
    }

    /// <summary>
    /// Gets the statements. Can be empty.
    /// </summary>
    public IReadOnlyList<TransformStatement> Statements { get; }

    /// <summary>
    /// Applies the inner <see cref="Statements"/>.
    /// </summary>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="editor">The code to transform.</param>
    /// <returns>True on success, false if any of the inner statement failed.</returns>
    public override bool Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        bool success = true;
        foreach( var statement in Statements )
        {
            success &= statement.Apply( monitor, editor );
        }
        return success;
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
        var statements = new List<TransformStatement>();
        head.MatchToken( "begin" );
        int begSpan = head.LastTokenIndex;
        Token? foundEnd = null;
        while( head.EndOfInput == null && !head.TryAcceptToken( "end", out foundEnd ) )
        {
            var s = cLang.TransformStatementAnalyzer.ParseStatement( cLang, ref head );
            if( s != null )
            {
                statements.Add( s );
            }
            else
            {
                head.AppendError( $"Failed to parse a transform '{cLang.LanguageName}' language statement.", -1 );
                break;
            }
        }
        if( foundEnd == null ) head.AppendError( "Expected 'end'.", 0 );
        return head.AddSpan( new TransformStatementBlock( begSpan, head.LastTokenIndex + 1, statements ) );
    }


}
