using CK.Core;
using CK.Transform.Core;
using System;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Handles trivia extension point match in a way that enables reuse of <see cref="LocationInserter"/>
/// algorithm and code.
/// </summary>
sealed partial class TriviaInjectionPointMatcher
{
    readonly IActivityMonitor _monitor;
    readonly InjectionPoint _injectionPoint;
    readonly RawString _injectedCode;
    bool _foundInsertPoint;
    bool _foundOpening;
    bool _foundOpeningIsAutoClosing;
    bool _foundOpeningIsRevert;

    public TriviaInjectionPointMatcher( IActivityMonitor monitor, InjectionPoint injectionPoint, RawString injectedCode )
    {
        _monitor = monitor;
        _injectionPoint = injectionPoint;
        _injectedCode = injectedCode;
    }

    /// <summary>
    /// Gets the injection point.
    /// </summary>
    public InjectionPoint InjectionPoint => _injectionPoint;

    /// <summary>
    /// Gets the text to insert before the match.
    /// </summary>
    public string? TextBefore { get; private set; }

    /// <summary>
    /// Gets the text to insert after the match.
    /// </summary>
    public string? TextAfter { get; private set; }

    /// <summary>
    /// Gets the 3 trivias that must replace the matched one.
    /// </summary>
    public Trivia[]? TextReplace { get; private set; }

    /// <summary>
    /// Tests whether the given trivia matches. This is called multiple times and the internal state is updated.
    /// On final call and success, true is returned and <see cref="TextBefore"/>, <see cref="TextAfter"/>
    /// and <see cref="TextReplace"/> are updated to reflect the action that should be applied.
    /// </summary>
    /// <param name="t">The trivia to process.</param>
    /// <returns>True on eventual success, false otherwise.</returns>
    public bool Match( Trivia t )
    {
        if( t.IsWhitespace ) return false;

        var sTrivia = t.Content.Span;
        var commentPrefix = sTrivia.Slice( 0, t.CommentStartLength );
        Throw.DebugAssert( commentPrefix.Length > 0 );

        ReadOnlySpan<char> commentSuffix;
        if( t.CommentEndLength > 0 )
        {
            commentSuffix = sTrivia.Slice( sTrivia.Length - t.CommentEndLength );
        }
        else
        {
            // Should be an InferredNewLine that result from the parsing...
            // For the moment, we consider the environment one, but this is not right.
            commentSuffix = Environment.NewLine;
        }
        if( !Parse( sTrivia, commentPrefix,
                    out bool isClosing,
                    out ReadOnlySpan<char> injectDef,
                    out bool isRevert,
                    out bool isAutoClosing ) )
        {
            return false;
        }
        // See comment above about the InferredNewLine...
        var injected = String.Join( Environment.NewLine, _injectedCode.Lines );

        // Check injection point syntax.
        if( isClosing && isAutoClosing ) _monitor.Error( $"Invalid extension tag: '{t.Content}': can not start with '</' and end with '/>'." );
        else if( isClosing && isRevert ) _monitor.Error( $"Invalid extension tag: '{t.Content}': revert must be on the opening tag." );
        else
        {
            if( isClosing )
            {
                if( !_foundOpening ) _monitor.Error( $"Closing '{t.Content}' has no opening." );
                else if( _foundInsertPoint )
                {
                    if( _foundOpeningIsAutoClosing )
                    {
                        _monitor.Error( $"Unexpected closing of extension tag: '{t.Content}': opening tag is auto closed (ends with '/>')." );
                    }
                    else if( !_foundOpeningIsRevert )
                    {
                        _monitor.Error( $"Unexpected closing of extension tag: '{t.Content}': it is already closed." );
                    }
                    // => This is the closing tag of a reverted extension.
                }
                else
                {
                    TextBefore = injected;
                    return _foundInsertPoint = true;
                }
            }
            else // Opening tag.
            {
                if( _foundOpening ) _monitor.Error( $"Duplicate opening of extension tag: '{_injectionPoint.Text}'." );
                else
                {
                    _foundOpening = true;
                    _foundOpeningIsAutoClosing = isAutoClosing;
                    _foundOpeningIsRevert = isRevert;
                    if( isAutoClosing )
                    {
                        TextReplace = new[]
                        {
                            new Trivia( t.TokenType, $"{commentPrefix}<{injectDef}>{commentSuffix}" ),
                            new Trivia( NodeType.Whitespace, injected ),
                            new Trivia( t.TokenType, $"{commentPrefix}</{_injectionPoint.Name}>{commentSuffix}" )
                        };
                        return _foundInsertPoint = true;
                    }
                    else
                    {
                        if( isRevert )
                        {
                            TextAfter = injected;
                            return _foundInsertPoint = true;
                        }
                    }
                }
            }
        }
        return false;
    }

    bool Parse( ReadOnlySpan<char> sTrivia,
                ReadOnlySpan<char> commentPrefix,
                out bool isClosing,
                out ReadOnlySpan<char> injectDef,
                out bool isRevert,
                out bool isAutoClosing )
    {
        isClosing = false;
        injectDef = default;
        isRevert = false;
        isAutoClosing = false;

        var head = sTrivia.Slice( commentPrefix.Length ).TrimStart(); // Skip "//\s*".
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
        head = head.Slice( isClosing ? 2 : 1 );
        // Name match the <InjectionPoint>.
        int nameLen = GetInsertionPointLength( head );
        if( nameLen == 0 || !head.TryMatch( _injectionPoint.Name ) ) return false;
        head = head.TrimStart();
        // "revert(t|se)"
        isRevert = head.TryMatch( "revert", StringComparison.OrdinalIgnoreCase ) || head.TryMatch( "reverse", StringComparison.OrdinalIgnoreCase );
        if( injectDef.Length > 0 )
        {
            injectDef.Overlaps( head, out int injectDefLength );
            Throw.DebugAssert( "Name + at least one whitesape + at least revert.Length", injectDefLength > nameLen + 1 + 6 );
            injectDef = injectDef.Slice( 0, injectDefLength );
        }
        if( isRevert ) head = head.TrimStart();
        if( head.Length == 0 ) return false;
        // Ends with "/>" or ">".
        isAutoClosing = head[0] == '/';
        if( isAutoClosing ) head = head.Slice( 1 );
        return head.Length > 0 && head[0] == '>';
    }

    internal static int GetInsertionPointLength( ReadOnlySpan<char> sHead )
    {
        int iS = 0;
        while( ++iS < sHead.Length && char.IsAsciiLetterOrDigit( sHead[iS] ) ) ;
        return iS;
    }
}
