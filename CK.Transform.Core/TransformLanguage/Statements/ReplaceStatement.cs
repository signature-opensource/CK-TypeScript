using CK.Core;
using System;
using static CK.Core.ActivityMonitor;

namespace CK.Transform.Core;

public sealed class ReplaceStatement : TransformStatement
{
    ReplaceStatement( int beg, int end ) : base( beg, end )
    {
    }

    public override bool Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        throw new NotImplementedException();
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
        return hasWith && replacement != null
                ? head.AddSpan( new ReplaceStatement( begSpan, head.LastTokenIndex + 1 ) )
                : null;

    }
}
