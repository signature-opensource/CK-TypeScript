using CK.Core;
using System;
using System.Collections.Immutable;

namespace CK.Transform.Core;

/// <summary>
/// An injection point is a <see cref="Token"/> (its type is <see cref="TokenType.GenericIdentifier"/>).
/// Its <see cref="Name"/> is defined between angle brackets: &lt;Name&gt; (syntax: "&lt;[letter or digit or .]+&gt;").
/// <para>
/// This is also used to capture target langage name (like in <c>create &lt;typescript&gt; transformer ...</c> or
/// <c>create &lt;sql.t&gt; transformer ...</c>).  
/// </para>
/// </summary>
public sealed class InjectionPoint : Token
{
    internal InjectionPoint( ReadOnlyMemory<char> text,
                             ImmutableArray<Trivia> leading = default,
                             ImmutableArray<Trivia> trailing = default )
        : base( TokenType.GenericIdentifier, leading, text, trailing )
    {
    }

    /// <summary>
    /// Models the different forms that an injection point can take.
    /// <see cref="GetString(ReadOnlySpan{char}, Kind)"/> can be used to create the string.
    /// <para>
    /// In the transform language, only the <see cref="Definition"/> is handled. The other kind
    /// are handled by <see cref="InjectionPointFinder"/>.
    /// </para>
    /// </summary>
    public enum Kind
    {
        /// <summary>
        /// <c>&lt;Name&gt;</c> is also the <see cref="Definition"/> (in the transform language).
        /// </summary>
        Opened,

        /// <summary>
        /// <c>&lt;Name revert &gt;</c>
        /// </summary>
        OpenedRevert,

        /// <summary>
        /// <c>&lt;Name /&gt;</c>
        /// </summary>
        AutoClosing,

        /// <summary>
        /// <c>&lt;Name revert /&gt;</c>
        /// </summary>
        AutoClosingRevert,

        /// <summary>
        /// <c>&lt;/Name&gt;</c>
        /// </summary>
        Closing,

        /// <summary>
        /// Definition of an injection point is the <c>&lt;Name&gt;</c> opened form.
        /// </summary>
        Definition = Opened
    }

    /// <summary>
    /// Get the injection point name without enclosing angle brackets 
    /// (the <see cref="Token.Text"/> has the brackets).
    /// </summary>
    public ReadOnlySpan<char> Name => Text.Span.Slice( 1, Text.Length - 2 );

    /// <summary>
    /// Helper that forwards on <see cref="char.IsAsciiLetterOrDigit"/> or '.'.
    /// </summary>
    /// <param name="sHead">The head.</param>
    /// <returns>The number of consecutive letter or digits found.</returns>
    public static int GetInjectionPointLength( ReadOnlySpan<char> sHead )
    {
        int iS = 0;
        while( ++iS < sHead.Length && (sHead[iS] == '.' || char.IsAsciiLetterOrDigit( sHead[iS] )) ) ;
        return iS;
    }

    /// <summary>
    /// Gets a string from a <see cref="Name"/> and a <see cref="Kind"/>.
    /// </summary>
    /// <param name="name">The injection point name. Must not be empty.</param>
    /// <param name="kind">The kind to create.</param>
    /// <returns>The resulting string.</returns>
    public static string GetString( ReadOnlySpan<char> name, Kind kind )
    {
        Throw.CheckArgument( name.Length > 0 );
        return kind switch
        {
            Kind.OpenedRevert => $"<{name} revert>",
            Kind.AutoClosing => $"<{name} />",
            Kind.AutoClosingRevert => $"<{name} revert />",
            Kind.Closing => $"</{name}>",
            _ => $"<{name}>",
        };
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
