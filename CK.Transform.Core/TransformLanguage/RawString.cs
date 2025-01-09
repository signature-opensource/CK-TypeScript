using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// A raw string follows the same rules as the C# raw string: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/raw-string.
/// <list type="bullet">
///     <item>A single quote opens a <c>"single-line string"</c>: the closing quote must appear before the end-of-line.</item>
///     <item>Two consecutive quotes is the empty string <c>""</c>.</item>
///     <item>
///     Three quotes or more opens a single or multi-line string that can contain consecutive quotes sequence shorter than the opening quotes.
///     (This string can be on a single line: """Hello "World"!""" is valid.)
///     </item>
/// </list>
/// <para>
/// Escape character '\' is NOT handled in a single-line string. If a '"' must apear, use a multi-quoted string.
/// </para>
/// <para>
/// Note that in C#, <c>"""raw "string""""</c> is not valid but this is valid for us (the string is <c>raw "string"</c>).
/// </para>
/// </summary>
public sealed class RawString : Token
{
    readonly ReadOnlyMemory<char> _innerText;
    readonly ImmutableArray<ReadOnlyMemory<char>> _lines;
    ImmutableArray<string> _sLines;

    // Single-line.
    internal RawString( ReadOnlyMemory<char> text,
                        ReadOnlyMemory<char> innerText,
                        ImmutableArray<Trivia> leading,
                        ImmutableArray<Trivia> trailing )
        : base( TokenType.GenericString, leading, text, trailing )
    {
        Throw.DebugAssert( text.Length > innerText.Length && text.Span.Contains( innerText.Span, StringComparison.Ordinal ) );
        _innerText = innerText;
        var s = innerText.Span;
        _lines = [innerText];
    }

    internal RawString( ReadOnlyMemory<char> text,
                        ReadOnlyMemory<char> innerText,
                        ImmutableArray<ReadOnlyMemory<char>> memoryLines,
                        ImmutableArray<Trivia> leading,
                        ImmutableArray<Trivia> trailing )
        : base( TokenType.GenericString, leading, text, trailing )
    {
        Throw.DebugAssert( text.Length > innerText.Length && text.Span.Contains( innerText.Span, StringComparison.Ordinal ) );
        _innerText = innerText;
        _lines = memoryLines;
    }

    /// <summary>
    /// Gets the actual text string without the enclosing quotes and no processing.
    /// </summary>
    public ReadOnlyMemory<char> InnerText => _innerText;

    /// <summary>
    /// Gets the lines.
    /// </summary>
    public ImmutableArray<ReadOnlyMemory<char>> MemoryLines => _lines;

    /// <summary>
    /// Gets the lines as strings.
    /// </summary>
    public ImmutableArray<string> Lines
    {
        get
        {
            if( _sLines.IsDefault )
            {
                _sLines = _lines.Select( l => l.ToString() ).ToImmutableArray();
            }
            return _sLines;
        }
    }
}
