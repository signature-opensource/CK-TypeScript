using CK.Core;
using System;
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
    /// Parsed by <see cref="TransformLanguageAnalyzer.ParseStatements(ref TokenizerHead)"/>.
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
    /// Applies the inner <see cref="Statements"/> until <see cref="SourceCodeEditor.HasError"/>
    /// becomes true.
    /// </remarks>
    public override void Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        if( !editor.HasError )
        {
            foreach( var statement in Statements )
            {
                statement.Apply( monitor, editor );
                if( editor.HasError ) break;
            }
        }
    }

    internal static TransformStatement? ParseBlockOrStatement( TransformLanguageAnalyzer analyzer, ref TokenizerHead head )
    {
        return head.LowLevelTokenType == TokenType.GenericIdentifier
               && head.LowLevelTokenText.Equals("begin", StringComparison.Ordinal)
                    ? Parse( analyzer, ref head )
                    : ParseRequiredStatement( analyzer, ref head );
    }

    /// <summary>
    /// Parses a "begin ... end" block.
    /// Statements are parsed by <see cref="TransformLanguageAnalyzer.ParseStatement(ref TokenizerHead)"/>.
    /// </summary>
    /// <param name="analyzer">Calling analyzer.</param>
    /// <param name="head">The head.</param>
    /// <returns>The list of transform statements.</returns>
    internal static TransformStatementBlock Parse( TransformLanguageAnalyzer analyzer, ref TokenizerHead head )
    {
        head.MatchToken( "begin" );
        int begSpan = head.LastTokenIndex;
        Token? foundEnd = null;
        while( head.EndOfInput == null && !head.TryAcceptToken( "end", out foundEnd ) )
        {
            if( ParseRequiredStatement( analyzer, ref head ) == null )
            {
                break;
            }
        }
        if( foundEnd == null && !head.TryAcceptToken( "end", out foundEnd ) )
        {
            head.AppendError( "Expected 'end'.", 0 );
        }
        return head.AddSpan( new TransformStatementBlock( begSpan, head.LastTokenIndex + 1 ) );
    }

    static TransformStatement? ParseRequiredStatement( TransformLanguageAnalyzer analyzer, ref TokenizerHead head )
    {
        var s = analyzer.ParseStatement( ref head );
        Throw.DebugAssert( s == null || s.CheckValid() );
        if( s == null )
        {
            head.AppendError( $"Failed to parse a '{analyzer.LanguageName}' language statement.", -1 );
        }
        return s;
    }
}
