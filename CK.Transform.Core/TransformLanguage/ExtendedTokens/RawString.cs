using CK.Core;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// A raw string is a <see cref="Token"/> that follows the same rules as the C# raw string:
/// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/raw-string.
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
/// However, it may be more readable to use a multiple line raw string syntax (without a last blank line) when
/// quotes occur at the end. This is the same string:
/// <code>
///     (note that it is the position of the closing quotes that drives the spaces before the lines) """
///     raw "string"
///     """
/// </code>
/// </para>
/// </summary>
public sealed class RawString : Token
{
    readonly ReadOnlyMemory<char> _innerText;
    readonly ImmutableArray<ReadOnlyMemory<char>> _lines;
    ImmutableArray<string> _sLines;
    string? _textLines;

    // Single-line.
    RawString( ReadOnlyMemory<char> text,
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

    RawString( ReadOnlyMemory<char> text,
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
    /// Gets the actual text string without the enclosing quotes and no processing:
    /// all new lines and white spaces appear. Use <see cref="Lines"/> or <see cref="MemoryLines"/>
    /// to obtain the final lines or <see cref="TextLines"/> for the lines as a string.
    /// </summary>
    public ReadOnlyMemory<char> InnerText => _innerText;

    /// <summary>
    /// Gets the lines.
    /// </summary>
    public ImmutableArray<ReadOnlyMemory<char>> MemoryLines => _lines;

    /// <summary>
    /// Gets the length of the surrounding quotes.
    /// </summary>
    public int QuoteLength => (Text.Length - _innerText.Length) / 2;

    /// <summary>
    /// Gets the opening quotes.
    /// </summary>
    public ReadOnlySpan<char> OpeningQuotes => Text.Span.Slice( QuoteLength );

    /// <summary>
    /// Gets the closing quotes.
    /// </summary>
    public ReadOnlySpan<char> ClosingQuotes => Text.Span[..^QuoteLength];

    /// <summary>
    /// Gets the final lines as a string joined with <see cref="Environment.NewLine"/>.
    /// </summary>
    public string TextLines
    {
        get
        {
            if( _textLines == null )
            {
                if( _lines.Length == 1 ) _textLines = _lines[0].ToString();
                else _textLines = string.Join( Environment.NewLine, Lines );
            }
            return _textLines;
        }
    }

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

    /// <summary>
    /// Matches a <see cref="RawString"/> between "double quotes". The current <see cref="TokenizerHead.LowLevelTokenType"/> must
    /// be <see cref="TokenType.DoubleQuote"/> otherwise an <see cref="ArgumentException"/> is thrown.
    /// </summary>
    /// <param name="head">The tokenizer head.</param>
    /// <param name="maxLineCount">Optional maximal line count: using 1 allows only a single line string.</param>
    /// <returns>The RawString on success, null if an error has been emitted.</returns>
    public static RawString? Match( ref TokenizerHead head, int maxLineCount = 0 )
    {
        Throw.CheckArgument( head.LowLevelTokenType is TokenType.DoubleQuote );
        return MatchAnyQuote( ref head, '"', '"', maxLineCount );
    }

    /// <summary>
    /// Matches a <see cref="RawString"/> that can use any quote characters.
    /// <para>
    /// The head must start with <paramref name="beqQuote"/> otherwise an <see cref="ArgumentException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="head">The tokenizer head.</param>
    /// <param name="beqQuote">The opening quote character.</param>
    /// <param name="endQuote">The closing quote character.</param>
    /// <param name="maxLineCount">Optional maximal line count: using 1 allows only a single line string.</param>
    /// <returns>The RawString on success, null if an error has been emitted.</returns>
    public static RawString? MatchAnyQuote( ref TokenizerHead head, char beqQuote, char endQuote, int maxLineCount = 0 )
    {
        var start = head.Head.TrimStart( beqQuote );
        var quoteCount = head.Head.Length - start.Length;
        if( quoteCount == 0 )
        {
            Throw.ArgumentException( nameof( head ), $"Expecting head to start with the beginning quote '{beqQuote}' character." );
        }
        Throw.DebugAssert( quoteCount > 0 );
        // Empty string.
        if( quoteCount == 2 )
        {
            head.PreAcceptToken( 2, out var text, out var leading, out var trailing );
            return head.Accept( new RawString( text, default, leading, trailing ) );
        }
        if( quoteCount == 1 )
        {
            return SingleLine( ref head, start, endQuote );
        }
        return PossiblyMultiLine( ref head, start, quoteCount, maxLineCount, endQuote );

        static RawString? SingleLine( ref TokenizerHead head, ReadOnlySpan<char> start, char endQuote )
        {
            int idxE = start.IndexOf( endQuote );
            if( idxE < 0 )
            {
                return UnterminatedString( ref head, endQuote );
            }
            start = start.Slice( 0, idxE );
            if( start.Contains( '\n' ) )
            {
                head.AppendError( "Single-line string must not contain end of line.", idxE + 2 );
                return null;
            }
            head.PreAcceptToken( idxE + 2, out var text, out var leading, out var trailing );
            return head.Accept( new RawString( text, text.Slice( 1, start.Length ), leading, trailing ) );
        }

        static RawString? PossiblyMultiLine( ref TokenizerHead head,
                                             ReadOnlySpan<char> start,
                                             int quoteCount,
                                             int maxLineCount,
                                             char endQuote )
        {
            Span<char> endingQuotes = stackalloc char[quoteCount];
            endingQuotes.Fill( endQuote );
            int idxE = start.IndexOf( endingQuotes );
            if( idxE < 0 )
            {
                return UnterminatedString( ref head, endQuote );
            }
            var lineOrMultiLine = start.Slice( 0, idxE );
            int idxFirstEndOfLine = lineOrMultiLine.IndexOf( "\n" );
            if( idxFirstEndOfLine >= 0 )
            {
                return MultiLine( ref head, lineOrMultiLine, quoteCount, idxFirstEndOfLine, maxLineCount );
            }
            // Single line case.
            int idxEndQuotes = idxE + quoteCount;
            // Kindly offset the end to handle """raw ""string""""" as |raw ""string""|.
            int offset = 0;
            while( idxEndQuotes < start.Length && start[idxEndQuotes] == endQuote )
            {
                idxEndQuotes++;
                if( ++offset >= quoteCount )
                {
                    head.AppendError( $"Invalid raw string terminator: too many closing {endQuote}.", idxE + 2 * quoteCount );
                    var tooMuch = offset - quoteCount + 1;
                    head.AppendError( $"{tooMuch} exceeding {endQuote}.", tooMuch );
                    return null;
                }
            }
            idxE += offset;
            start = start.Slice( 0, idxE );
            head.PreAcceptToken( 2 * quoteCount + start.Length, out var text, out var leading, out var trailing );
            return head.Accept( new RawString( text, text.Slice( quoteCount, start.Length ), leading, trailing ) );
        }

        static RawString? MultiLine( ref TokenizerHead head, ReadOnlySpan<char> multiLine, int quoteCount, int idxFirstEndOfLine, int maxLineCount )
        {
            int contentLength = multiLine.Length;
            int tokenLength = 2 * quoteCount + contentLength;
            ReadOnlySpan<char> mustBeEmpty;
            if( idxFirstEndOfLine > 0 )
            {
                mustBeEmpty = multiLine.Slice( 0, idxFirstEndOfLine );
                if( mustBeEmpty.ContainsAnyExcept( " \r\t" ) )
                {
                    head.AppendError( $"Invalid multi-line raw string: there must be no character after the opening {head.Head.Slice( 0, quoteCount )} characters.", tokenLength );
                    return null;
                }
                multiLine = multiLine.Slice( idxFirstEndOfLine + 1 );
            }
            int idxLastEndOfLine = multiLine.LastIndexOf( '\n' );
            if( idxLastEndOfLine < 0 )
            {
                head.AppendError( $"Invalid multi-line raw string: at least one line must appear between the {head.Head.Slice( 0, quoteCount )}.", tokenLength );
                return null;
            }
            mustBeEmpty = multiLine.Slice( idxLastEndOfLine + 1 );
            if( mustBeEmpty.ContainsAnyExcept( " \t" ) )
            {
                head.AppendError( $"Invalid multi-line raw string: there must be no character on the line before the closing {head.Head.Slice( 0, quoteCount )} characters.", tokenLength );
                return null;
            }
            int prefixLength = mustBeEmpty.Length;
            multiLine = multiLine.Slice( 0, multiLine.Length - prefixLength );
            var mLine = head.Text.Slice( head.Text.Length - head.Head.Length + quoteCount + idxFirstEndOfLine + 1, multiLine.Length );
            var builder = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();

            // EnumerateLines normalizes the EOL. One cannot update a position
            // without inspecting the actual EOL, so we use Overlaps to obtain
            // the offset in the ReadOnlyMemory.
            bool hasEmptyLine = false;
            foreach( var line in multiLine.EnumerateLines() )
            {
                if( hasEmptyLine )
                {
                    builder.Add( default );
                    hasEmptyLine = false;
                }
                if( line.Length > prefixLength )
                {
                    if( line.Slice( 0, prefixLength ).ContainsAnyExcept( " \t" ) )
                    {
                        head.AppendError( $"Invalid multi-line raw string: there must be no character before column {prefixLength} in '{line}'.", tokenLength );
                        return null;
                    }
                    Throw.DebugAssert( multiLine.Overlaps( line ) );
                    multiLine.Overlaps( line, out var pos );
                    builder.Add( mLine.Slice( pos + prefixLength, line.Length - prefixLength ) );
                }
                else
                {
                    if( line.ContainsAnyExcept( " \t" ) )
                    {
                        head.AppendError( $"Invalid multi-line raw string: there must be no character before column {prefixLength}.", tokenLength );
                        return null;
                    }
                    hasEmptyLine = true;
                }
            }
            if( maxLineCount > 0 && builder.Count > maxLineCount )
            {
                head.AppendError( maxLineCount == 1
                                    ? $"Expected single line (found {builder.Count} lines)."
                                    : $"Expected at most {maxLineCount} lines (found {builder.Count} lines).", tokenLength );
                return null;
            }
            head.PreAcceptToken( tokenLength, out var text, out var leading, out var trailing );
            return head.Accept( new RawString( text, text.Slice( quoteCount, contentLength - quoteCount ), builder.DrainToImmutable(), leading, trailing ) );
        }
    }

    private static RawString? UnterminatedString( ref TokenizerHead head, char quote )
    {
        head.AppendError( $"Unterminated string (quote is {quote}).", head.Head.Length );
        return null;
    }
}
