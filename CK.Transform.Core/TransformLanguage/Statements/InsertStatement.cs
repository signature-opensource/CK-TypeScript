using CK.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

/// <summary>
/// Insert statement: <c>insert "..." [after|before] [*|<see cref="LocationMatcher"/>];</c>
/// or <c>insert [after|before] [*|<see cref="LocationMatcher"/>] "...";</c>.
/// </summary>
public sealed class InsertStatement : TransformStatement
{
    bool _isBefore;
    RawString? _insertText;

    InsertStatement( int beg, int end, bool isBefore, RawString insertText )
        : base( beg, end )
    {
        _isBefore = isBefore;
        _insertText = insertText;
    }

    [MemberNotNullWhen( true, nameof( _insertText ) )]
    public override bool CheckValid()
    {
        return base.CheckValid() && _insertText != null;
    }

    /// <summary>
    /// Gets the optional matcher. When null it is a "insert ... [before|after] *" statement.
    /// </summary>
    public LocationMatcher? Matcher => FirstChild as LocationMatcher;

    /// <summary>
    /// Gets whether the text is inserted before or after the <see cref="Matcher"/>
    /// </summary>
    public bool IsBefore => _isBefore;

    /// <inheritdoc />
    public override void Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        Throw.DebugAssert( CheckValid() );
        int filterCount = editor.PushTokenOperator( Matcher );
        try
        {
            bool applied = false;
            using( var e = editor.OpenScopedEditor() )
            {
                while( e.Tokens.NextEach( skipEmpty: true ) )
                {
                    while( e.Tokens.NextMatch( out var first, out var last, out var count ) )
                    {
                        var insert = new Token( TokenType.GenericAny,
                                                Trivia.Empty,
                                                _insertText.TextLines,
                                                Trivia.Empty );
                        if( _isBefore )
                        {
                            e.InsertBefore( first.Index, insert );
                        }
                        else
                        {
                            e.InsertAt( last.Index + 1, insert );
                        }
                        applied = true;
                    }
                }
            }
            if( applied ) editor.SetNeedReparse();
        }
        finally
        {
            editor.PopTokenOperator( filterCount );
        }
    }

    internal static InsertStatement? Parse( TransformLanguageAnalyzer analyzer, ref TokenizerHead head, Token replaceToken )
    {
        Throw.DebugAssert( replaceToken.Text.Span.Equals( "insert", StringComparison.Ordinal ) );
        int begSpan = head.LastTokenIndex;

        RawString? insertText = null;
        if( head.LowLevelTokenType == TokenType.DoubleQuote )
        {
            insertText = RawString.Match( ref head );
            if( insertText == null ) return null;
        }

        bool isBefore = head.TryAcceptToken( "before", out var _ );
        if( !isBefore && head.MatchToken( "after" ) is TokenError )
        {
            return null;
        }

        LocationMatcher? matcher = null;
        bool hasMatcher = false;
        if( head.LowLevelTokenType is TokenType.Asterisk )
        {
            head.AcceptLowLevelToken();
            hasMatcher = true;
        }
        else
        {
            matcher = LocationMatcher.Parse( analyzer, ref head );
            hasMatcher = matcher != null;
        }
        if( !hasMatcher )
        {
            head.AppendError( "Expected pattern or * (all)", 0 );
        }

        if( insertText == null && head.LowLevelTokenType == TokenType.DoubleQuote )
        {
            insertText = RawString.Match( ref head );
        }
        if( hasMatcher && insertText != null )
        {
            head.TryAcceptToken( TokenType.SemiColon, out _ );
            return head.AddSpan( new InsertStatement( begSpan, head.LastTokenIndex + 1, isBefore, insertText ) );
        }
        return null;
    }
}
