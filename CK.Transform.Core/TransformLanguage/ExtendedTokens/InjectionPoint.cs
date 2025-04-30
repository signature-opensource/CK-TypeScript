using System;
using System.Collections.Immutable;

namespace CK.Transform.Core;

/// <summary>
/// An injection point is a <see cref="Token"/>.
/// Its <see cref="Name"/> is defined between angle brackets: &lt;Name&gt;.
/// <para>
/// This token is not handled as a low-level token (by the <see cref="ILowLevelTokenizer"/>).
/// It could have been because transform language currently doesn't use the &lt; character
/// for anything else than the injection point.
/// Handling it at the analyzer level has the merit to challenge it (testing the
/// pattern "&lt;[letter or digit]+&gt;" only when needed (in <see cref="InjectIntoStatement"/> parsing)
/// but has the drawback to be explicitly handled by the <see cref="ITargetAnalyzer.CreateSpanMatcher(CK.Core.IActivityMonitor, ReadOnlySpan{char}, ReadOnlyMemory{char})"/>
/// pattern parser.
/// </para>
/// </summary>
public sealed class InjectionPoint : Token
{
    internal InjectionPoint( ReadOnlyMemory<char> text, ImmutableArray<Trivia> leading = default, ImmutableArray<Trivia> trailing = default )
        : base( TokenType.GenericIdentifier, leading, text, trailing )
    {
    }

    /// <summary>
    /// Get the injection point name without enclosing angle brackets 
    /// (the <see cref="Token.Text"/> has the brackets).
    /// </summary>
    public ReadOnlySpan<char> Name => Text.Span.Slice( 1, Text.Length - 2 );

    /// <summary>
    /// Helper that forwards on <see cref="char.IsAsciiLetterOrDigit"/>.
    /// </summary>
    /// <param name="sHead">The head.</param>
    /// <returns>The number of consecutive letter or digits found.</returns>
    public static int GetInjectionPointLength( ReadOnlySpan<char> sHead )
    {
        int iS = 0;
        while( ++iS < sHead.Length && char.IsAsciiLetterOrDigit( sHead[iS] ) ) ;
        return iS;
    }

    /// <summary>
    /// Creates a &lt;InjectionPoint&gt; token or appends an error.
    /// </summary>
    /// <param name="head">The tokenizer head.</param>
    /// <returns>The token or null if not matched.</returns>
    public static InjectionPoint? Match( ref TokenizerHead head )
    {
        var i = TryMatch( ref head );
        if( i == null ) head.AppendError( "Expected <InjectionPoint>.", 0 );
        return i;
    }

    /// <summary>
    /// Tries to match a &lt;InjectionPoint&gt; token without any error.
    /// </summary>
    /// <param name="head">The tokenizer head.</param>
    /// <returns>The token or null if not matched.</returns>
    public static InjectionPoint? TryMatch( ref TokenizerHead head )
    {
        if( head.LowLevelTokenType == TokenType.LessThan )
        {
            var sHead = head.Head;
            int nameLen = GetInjectionPointLength( sHead );
            if( nameLen > 0 && nameLen < sHead.Length && sHead[nameLen] == '>' )
            {
                head.PreAcceptToken( nameLen + 1, out var text, out var leading, out var trailing );
                return head.Accept( new InjectionPoint( text, leading, trailing ) );
            }
        }
        return null;
    }
}
