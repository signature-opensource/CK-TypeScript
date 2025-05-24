using CK.Core;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Serialization;

namespace CK.Transform.Core;

/// <summary>
/// Checks that the source code contains the specified "&lt;InjectionPoint&gt;".
/// and does nothing if it exists.
/// If the injection point is not present, the body is executed and the injection is
/// added at the start of the source code.
/// </summary>
public sealed class UnlessStatement : TransformStatement
{
    InjectionPoint _target;
    TransformStatement _body;
    readonly Trivia _marker;

    public UnlessStatement( int beg, int end, InjectionPoint target, TransformStatement body, Trivia marker )
        : base( beg, end )
    {
        _target = target;
        _body = body;
        _marker = marker;
    }

    /// <inheritdoc />
    public override void Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        var count = editor.PushTokenOperator( new InjectionPointFilterOperator( false, _target ) );
        _body.Apply( monitor, editor );
        using( var e = editor.OpenScopedEditor() )
        {
            while( e.Tokens.NextEach( skipEmpty: true ) )
            {
                while( e.Tokens.NextMatch( out var first, out var last, out _ ) )
                {
                    var leading = first.Token.LeadingTrivias.Prepend( _marker ).ToImmutableArray();
                    var t = first.Token.SetTrivias( leading );
                    e.Replace( first.Index, t );
                }
            }
        }
        editor.PopTokenOperator( count );
    }

    internal static UnlessStatement? Parse( TransformLanguageAnalyzer analyzer, ref TokenizerHead head, Token replaceToken )
    {
        Throw.DebugAssert( replaceToken.Text.Span.Equals( "unless", StringComparison.Ordinal ) );
        int begSpan = head.LastTokenIndex;
        var target = InjectionPoint.Match( ref head );
        var body = TransformStatementBlock.ParseBlockOrStatement( analyzer, ref head );
        if( target != null && body != null )
        {
            var marker = analyzer.TargetAnalyzer.CreateInjectionPointTrivia( target );
            return head.AddSpan( new UnlessStatement( begSpan, head.LastTokenIndex + 1, target, body, marker ) );
        }
        return null;
    }
}
