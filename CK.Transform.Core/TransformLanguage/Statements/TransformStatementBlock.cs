using CK.Core;
using System.Collections.Generic;

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
    internal TransformStatementBlock( int beg, int end, List<TransformStatement> statements )
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
}
