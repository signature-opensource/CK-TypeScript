using CK.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;


/// <summary>
/// This is a ref struct only to emphasize the fact that this is
/// simply an encapsulation of a state machine.
/// <para>
/// This can be used in 2 modes: to inject text (<see cref="InjectIntoStatement"/>) or
/// only to find an &lt;InjectionPoint&gt;.
/// </para>
/// <para>
/// This currently use a monitor to report error. This is not good and should be refactored.
/// </para>
/// </summary>
ref struct InjectionPointFinder
{
    readonly InjectionPoint _injectionPoint;
    readonly RawString? _injectedCode;
    string? _openingColumnPrefix;
    Trivia _injectOpening;
    Trivia _injectText;
    Trivia _injectClosing;
    bool _foundInsertPoint;
    bool _foundOpeningIsAutoClosing;
    bool _foundOpeningIsRevert;

    public InjectionPointFinder( InjectionPoint injectionPoint, RawString? injectedCode )
    {
        _injectionPoint = injectionPoint;
        _injectedCode = injectedCode;
    }

    /// <summary>
    /// Gets the whitespace string that offsets the opening "&lt;InjectionPoint /&gt;"
    /// or "&lt;InjectionPoint&gt;".
    /// </summary>
    public readonly string? OpeningColumnPrefix => _openingColumnPrefix;

    /// <summary>
    /// Gets whether the opening "&lt;InjectionPoint /&gt;"
    /// or "&lt;InjectionPoint&gt;" has been found.
    /// </summary>
    [MemberNotNullWhen( true, nameof( OpeningColumnPrefix ), nameof( _openingColumnPrefix ) )]
    public readonly bool FoundOpening => _openingColumnPrefix != null;

    /// <summary>
    /// Gets whether this is a reverted injection point.
    /// </summary>
    public readonly bool IsRevert => _foundOpeningIsRevert;

    /// <summary>
    /// Gets the trivia that must replace the opening one.
    /// <see cref="Trivia.IsDefault"/> when only the text must be inserted (the injection point is already opened).
    /// </summary>
    public readonly Trivia InjectOpening => _injectOpening;

    /// <summary>
    /// Gets the trivia to insert.
    /// <see cref="Trivia.IsDefault"/> when <see cref="FoundOpening"/> is false.
    /// </summary>
    public readonly Trivia InjectText => _injectText;

    /// <summary>
    /// Gets the trivia to inject after <see cref="InjectText"/>.
    /// <see cref="Trivia.IsDefault"/> when only the text must be inserted (the injection point is already opened).
    /// </summary>
    public readonly Trivia InjectClosing => _injectClosing;

    /// <summary>
    /// Tests whether the given trivia matches. This is called multiple times and the internal state is updated.
    /// On success, true is returned and the public readonly fields are updated to reflect the action that
    /// should be applied to the current token trivia.
    /// </summary>
    /// <param name="monitor">The monitor for errors.</param>
    /// <param name="t">The trivia to process.</param>
    /// <returns>True on eventual success, false otherwise.</returns>
    [MemberNotNullWhen( true, nameof( OpeningColumnPrefix ) )]
    public bool Match( IActivityMonitor monitor, Trivia t )
    {
        if( t.CommentStartLength == 0 
            || !ParseTrivia( t,
                             _injectionPoint.Name,
                             out bool isClosing,
                             out ReadOnlySpan<char> injectDef,
                             out bool isRevert,
                             out bool isAutoClosing ) )
        {
            return false;
        }
        // Check injection point syntax.
        if( isClosing && isAutoClosing ) monitor.Error( $"Invalid extension tag: '{t.Content}': can not start with '</' and end with '/>'." );
        else if( isClosing && isRevert ) monitor.Error( $"Invalid extension tag: '{t.Content}': revert must be on the opening tag." );
        else
        {
            if( isClosing )
            {
                if( !FoundOpening ) monitor.Error( $"Closing '{t.Content}' has no opening." );
                else if( _foundInsertPoint )
                {
                    if( _foundOpeningIsAutoClosing )
                    {
                        monitor.Error( $"Unexpected closing of extension tag: '{t.Content}': opening tag is auto closed (ends with '/>')." );
                    }
                    else if( !_foundOpeningIsRevert )
                    {
                        monitor.Error( $"Unexpected closing of extension tag: '{t.Content}': it is already closed." );
                    }
                    // => This is the closing tag of a reverted extension.
                }
                else
                {
                    return _foundInsertPoint = true;
                }
            }
            else // Opening tag.
            {
                if( FoundOpening ) monitor.Error( $"Duplicate opening of extension tag: '{_injectionPoint.Text}'." );
                else
                {
                    var cOffset = t.GetColumnNumber() - 1;
                    _openingColumnPrefix = cOffset > 0 ? new string( ' ', cOffset ) : "";
                    _foundOpeningIsAutoClosing = isAutoClosing;
                    _foundOpeningIsRevert = isRevert;
                    if( _injectedCode != null )
                    {
                        var text = String.Join( Environment.NewLine + OpeningColumnPrefix, _injectedCode.Lines );
                        // Block comment injection is "inline".
                        if( t.IsLineComment ) text += Environment.NewLine;
                        _injectText = new Trivia( TokenType.Whitespace, text );
                    }
                    if( isAutoClosing )
                    {
                        var tContent = t.Content.Span;
                        var commentPrefix = tContent.Slice( 0, t.CommentStartLength );
                        // Reproduce the block comment end or ending new line.
                        ReadOnlySpan<char> commentSuffix;
                        if( t.CommentEndLength > 0 )
                        {
                            commentSuffix = tContent.Slice( tContent.Length - t.CommentEndLength );
                        }
                        else
                        {
                            // Should be an InferredNewLine that result from the parsing...
                            // For the moment, we consider the environment one, but this is not right.
                            commentSuffix = Environment.NewLine;
                        }
                        _injectOpening = new Trivia( t.TokenType, $"{commentPrefix}<{injectDef}>{commentSuffix}" );
                        _injectClosing = new Trivia( t.TokenType, $"{commentPrefix}</{_injectionPoint.Name}>{commentSuffix}" );
                        return _foundInsertPoint = true;
                    }
                    else
                    {
                        if( isRevert )
                        {
                            return _foundInsertPoint = true;
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// It is useless to call this if <see cref="Trivia.CommentStartLength"/> is 0.
    /// </summary>
    internal static bool ParseTrivia( in Trivia trivia,
                                      ReadOnlySpan<char> injectionPointName,
                                      out bool isClosing,
                                      out ReadOnlySpan<char> injectDef,
                                      out bool isRevert,
                                      out bool isAutoClosing )
    {
        Throw.DebugAssert( injectionPointName.Length > 0 );
        isClosing = false;
        injectDef = default;
        isRevert = false;
        isAutoClosing = false;
        if( trivia.CommentStartLength == 0 ) return false;

        var head = trivia.Content.Span.Slice( trivia.CommentStartLength ).TrimStart(); // Skip "//\s*".
        if( head.Length < 3 ) return false; // At least "<X>".
        if( head[0] != '<' ) return false; // Starts with '<'.
        isClosing = head[1] == '/';
        if( isClosing )
        {
            head = head.Slice( 2 );
        }
        else
        {
            head = head.Slice( 1 );
            injectDef = head;
        }
        // Name match the <InjectionPoint>.
        int nameLen = InjectionPoint.GetInjectionPointLength( head );
        if( nameLen == 0 || !head.TryMatch( injectionPointName ) ) return false;
        head = head.TrimStart();
        // "revert"
        isRevert = head.TryMatch( "revert", StringComparison.OrdinalIgnoreCase );
        if( injectDef.Length > 0 )
        {
            // Capturing the opening tag this way supports any future "attribute". 
            injectDef.Overlaps( head, out int injectDefLength );
            injectDef = injectDef.Slice( 0, injectDefLength );
        }
        if( isRevert ) head = head.TrimStart();
        if( head.Length == 0 ) return false;
        // Ends with "/>" or ">".
        isAutoClosing = head[0] == '/';
        if( isAutoClosing ) head = head.Slice( 1 );
        return head.Length > 0 && head[0] == '>';
    }

}

