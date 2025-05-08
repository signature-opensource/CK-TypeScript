using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.Transform.Core;

public sealed class ReplaceStatement : TransformStatement
{
    RawString? _replacement;

    ReplaceStatement( int beg, int end, RawString replacement )
        : base( beg, end )
    {
        _replacement = replacement;
    }

    [MemberNotNullWhen( true, nameof( _replacement ) )]
    public override bool CheckValid()
    {
        return base.CheckValid() && _replacement != null;
    }

    /// <summary>
    /// Gets the optional matcher. When null it is a "replace * with ..." statement.
    /// </summary>
    public LocationMatcher? Matcher => Children.FirstChild as LocationMatcher;

    /// <inheritdoc />
    public override void Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        Throw.DebugAssert( CheckValid() );
        int filterCount = editor.PushTokenFilter( Matcher );
        try
        {
            bool applied = false;
            using( var e = editor.OpenEditor() )
            {
                foreach( var each in e.Tokens )
                    foreach( var range in each )
                    {
                        GetFirstLastAndCount( range, out var first, out var last, out var count );
                        var replace = new Token( TokenType.GenericAny,
                                                 first.Token.LeadingTrivias,
                                                 _replacement.TextLines,
                                                 last.Token.TrailingTrivias );
                        editor.Replace( first.Index, count, replace );
                        applied = true;
                    }
            }
            if( applied ) editor.SetNeedReparse();
        }
        finally
        {
            editor.PopTokenFilter( filterCount );
        }
    }

    static void GetFirstLastAndCount( IEnumerable<SourceToken> tokens, out SourceToken first, out SourceToken last, out int count )
    {
        using var e = tokens.GetEnumerator();
        Throw.CheckState( "IEnumerable<SourceToken> must never be empty.", e.MoveNext() );
        first = e.Current;
        last = first;
        count = 1;
        while( e.MoveNext() )
        {
            ++count;
            last = e.Current;
        }
    }

    internal static ReplaceStatement? Parse( LanguageTransformAnalyzer analyzer, ref TokenizerHead head, Token replaceToken )
    {
        Throw.DebugAssert( replaceToken.Text.Span.Equals( "replace", StringComparison.Ordinal ) );
        int begSpan = head.LastTokenIndex + 1;
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
        bool hasWith = head.MatchToken( "with" ) is Token;
        RawString? replacement = null;
        if( head.LowLevelTokenType == TokenType.DoubleQuote )
        {
            replacement = RawString.Match( ref head );
        }
        else
        {
            head.AppendError( "Expecting replacement string.", 0 );
        }
        if( hasMatcher && hasWith && replacement != null )
        {
            head.TryAcceptToken( TokenType.SemiColon, out _ );
            return head.AddSpan( new ReplaceStatement( begSpan, head.LastTokenIndex + 1, replacement ) );
        }
        return null;
    }
}
