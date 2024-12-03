using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// A raw string follows the same rules as the C# raw string: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/raw-string.
/// <list type="bullet">
///     <item>A single quote opens a "single-line string": the closing quote must appear before the end-of-line.</item>
///     <item>Two consecutive quotes is the empty string "".</item>
///     <item>
///     Three quotes or more opens a multi-line string that can contain consecutive quotes sequence shorter than the opening quotes.
///     (This string can be on a single line: """Hello "World"!""" is valid.)
///     </item>
/// </list>
/// <para>
/// Escape character '\' can be used only for a " in a single-line string: "\"" is a string with a " character but <c>"""\""""</c> is the string '\"'.
/// </para>
/// <para>
/// Note that in C#, <c>"""raw "string""""</c> is not valid (but valid for us).
/// </para>
/// </summary>
public sealed class RawString : TokenNode
{
    readonly ReadOnlyMemory<char> _innerText;
    readonly ImmutableArray<ReadOnlyMemory<char>> _lines;

    // Single-line.
    internal RawString( ReadOnlyMemory<char> text,
                        ReadOnlyMemory<char> singleLine,
                        ImmutableArray<Trivia> leading,
                        ImmutableArray<Trivia> trailing )
        : base( NodeType.GenericString, text, leading, trailing )
    {
        Throw.DebugAssert( text.Length > singleLine.Length && text.Span.Contains( singleLine.Span, StringComparison.Ordinal ) );
        _innerText = singleLine;
        var s = singleLine.Span;
        int c = s.Count( "\\\"" );
        if( c > 0 )
        {
            var eval = String.Create( s.Length - c, singleLine, ( content, line ) =>
            {
                int i;
                var s = line.Span;
                while( (i = s.IndexOf( "\\\"" )) >= 0 )
                {
                    s.Slice( 0, i ).CopyTo( content );
                    s = s.Slice( i + 1 );
                    content = content.Slice( i );
                }
                s.CopyTo( content );
            } );
            singleLine = eval.AsMemory();
        }
        _lines = [singleLine];
    }

    internal RawString( ReadOnlyMemory<char> text,
                        ReadOnlyMemory<char> multiLine,
                        int prefixLength,
                        ImmutableArray<Trivia> leading,
                        ImmutableArray<Trivia> trailing )
        : base( NodeType.GenericString, text, leading, trailing )
    {
        Throw.DebugAssert( text.Length > multiLine.Length && text.Span.Contains( multiLine.Span, StringComparison.Ordinal ) );
        _innerText = multiLine;
    }

    /// <summary>
    /// Gets the actual text string without the enclosing quotes.
    /// </summary>
    public ReadOnlyMemory<char> InnerText => _innerText;

    /// <summary>
    /// Gets the lines.
    /// </summary>
    public ImmutableArray<ReadOnlyMemory<char>> Lines => _lines;

}
