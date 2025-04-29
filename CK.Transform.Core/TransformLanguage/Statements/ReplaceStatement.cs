using CK.Core;
using System;
using System.Diagnostics;
using static CK.Core.ActivityMonitor;

namespace CK.Transform.Core;

public sealed class ReplaceStatement : TransformStatement
{
    readonly RawString _replacement;

    ReplaceStatement( int beg, int end, RawString replacement )
        : base( beg, end )
    {
        _replacement = replacement;
    }

    public LocationMatcher? Matcher => Children.FirstChild as LocationMatcher;

    public override bool Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        // foreach( var each in editor.SourceTokens )
        //    foreach( var range in each.Ranges )
        //       foreach( var t in range.SourceTokens )
        using var scope = editor.ScopedTokens.ApplyTokenFilter( Matcher );
        foreach( var t in editor.SourceTokens )
        {
            _replacement.InnerText
        }
    }

    internal static ReplaceStatement? Parse( TransformerHost.Language language, ref TokenizerHead head, Token replaceToken )
    {
        Throw.DebugAssert( replaceToken.Text.Span.Equals( "replace", StringComparison.Ordinal ) );
        int begSpan = head.LastTokenIndex + 1;
        LocationMatcher? matcher = LocationMatcher.Parse( ref head );
        if( matcher == null && head.MatchToken( TokenType.Asterisk, "pattern or * (all)" ) is TokenError )
        {
            return null;
        }
        bool hasWith = head.MatchToken( "with" ) is Token;
        RawString? replacement = null;
        if( head.LowLevelTokenType == TokenType.DoubleQuote )
        {
            replacement = RawString.TryMatch( ref head );
        }
        else
        {
            head.AppendError( "Expecting replacement string.", 0 );
        }
        if( hasWith && replacement != null )
        {
            head.TryAcceptToken( TokenType.SemiColon, out _ );
            return head.AddSpan( new ReplaceStatement( begSpan, head.LastTokenIndex + 1, replacement ) );
        }
        return null;
    }
}
