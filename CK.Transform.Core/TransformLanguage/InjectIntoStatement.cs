using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace CK.Transform.Core;

public sealed class InjectIntoStatement : TransformStatement
{
    public InjectIntoStatement( int beg, int end, RawString content, InjectionPoint target )
        : base( beg, end )
    {
        Content = content;
        Target = target;
    }

    public RawString Content { get; }

    public InjectionPoint Target { get; }

    public override bool Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        bool success = true;
        using( var _ = monitor.OnError( () => success = false ) )
        {
            SourceToken modified = default;

            // The finder will find the first match (or none) and will error on duplicate
            // or injection point. We need the same state machine for all the tokens and
            // process all the tokens (to detect duplicate errors).
            var finder = new InjectionPointFinder( Target, Content );
            foreach( var sourceToken in editor.SourceTokens )
            {
                var token = sourceToken.Token;
                var newTrivias = ProcessTrivias( monitor, ref finder, token.LeadingTrivias );
                if( !newTrivias.IsDefault )
                {
                    Throw.DebugAssert( modified.IsDefault );
                    modified = new SourceToken( token.SetTrivias( newTrivias ), sourceToken.Span, sourceToken.Index );
                }
                newTrivias = ProcessTrivias( monitor, ref finder, token.TrailingTrivias );
                if( !newTrivias.IsDefault )
                {
                    Throw.DebugAssert( modified.IsDefault );
                    modified = new SourceToken( token.SetTrivias( token.LeadingTrivias, newTrivias ), sourceToken.Span, sourceToken.Index );
                }
            }
            if( modified.IsDefault )
            {
                monitor.Error( $"Unable to find the injection point '{Target}'." );
            }
            else
            {
                editor.InPlaceReplace( modified.Index, modified.Token );
                editor.SetNeedReparse();
            }
        }
        return success;

        static ImmutableArray<Trivia> ProcessTrivias( IActivityMonitor monitor, ref InjectionPointFinder finder, ImmutableArray<Trivia> trivias )
        {
            ImmutableArray<Trivia> modified = default;
            // Here again, we don't stop the loop until all trivias have been detected
            // in order to detect duplicates.
            int idx = 0;
            foreach( var t in trivias )
            {
                if( finder.Match( monitor, t ) )
                {
                    Throw.DebugAssert( modified.IsDefault );
                    if( finder.TextBefore != null )
                    {
                        modified = trivias.Insert( idx, new Trivia( TokenType.Whitespace, finder.TextBefore ) );
                    }
                    else if( finder.TextAfter != null )
                    {
                        modified = trivias.Insert( idx + 1, new Trivia( TokenType.Whitespace, finder.TextAfter ) );
                    }
                    else
                    {
                        Throw.DebugAssert( finder.TextReplace != null );
                        if( trivias.Length == 1 )
                        {
                            modified = ImmutableCollectionsMarshal.AsImmutableArray( finder.TextReplace );
                        }
                        else
                        {
                            var a = new Trivia[trivias.Length + 2];
                            var s = trivias.AsSpan();
                            s.Slice( 0, idx ).CopyTo( a.AsSpan( 0, idx ) );
                            finder.TextReplace.CopyTo( a, idx );
                            s.Slice( idx + 1 ).CopyTo( a.AsSpan( idx + 3 ) );
                            modified = ImmutableCollectionsMarshal.AsImmutableArray( a );
                        }
                    }
                }
                ++idx;
            }
            return modified;
        }
    }

    // This is a ref struct only to emphasize the fact that this is
    // simply an encapsultation of a state machine.
    ref struct InjectionPointFinder
    {
        readonly InjectionPoint _injectionPoint;
        readonly RawString _injectedCode;
        bool _foundInsertPoint;
        bool _foundOpening;
        bool _foundOpeningIsAutoClosing;
        bool _foundOpeningIsRevert;

        public InjectionPointFinder( InjectionPoint injectionPoint, RawString injectedCode )
        {
            _injectionPoint = injectionPoint;
            _injectedCode = injectedCode;
        }

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
        /// On success, true is returned and <see cref="TextBefore"/>, <see cref="TextAfter"/>
        /// and <see cref="TextReplace"/> are updated to reflect the action that should be applied to the current token trivia.
        /// </summary>
        /// <param name="t">The trivia to process.</param>
        /// <returns>True on eventual success, false otherwise.</returns>
        public bool Match( IActivityMonitor monitor, Trivia t )
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
            if( isClosing && isAutoClosing ) monitor.Error( $"Invalid extension tag: '{t.Content}': can not start with '</' and end with '/>'." );
            else if( isClosing && isRevert ) monitor.Error( $"Invalid extension tag: '{t.Content}': revert must be on the opening tag." );
            else
            {
                if( isClosing )
                {
                    if( !_foundOpening ) monitor.Error( $"Closing '{t.Content}' has no opening." );
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
                        TextBefore = injected;
                        return _foundInsertPoint = true;
                    }
                }
                else // Opening tag.
                {
                    if( _foundOpening ) monitor.Error( $"Duplicate opening of extension tag: '{_injectionPoint.Text}'." );
                    else
                    {
                        _foundOpening = true;
                        _foundOpeningIsAutoClosing = isAutoClosing;
                        _foundOpeningIsRevert = isRevert;
                        if( isAutoClosing )
                        {
                            TextReplace =
                            [
                                new Trivia( t.TokenType, $"{commentPrefix}<{injectDef}>{commentSuffix}" ),
                                new Trivia( TokenType.Whitespace, injected ),
                                new Trivia( t.TokenType, $"{commentPrefix}</{_injectionPoint.Name}>{commentSuffix}" )
                            ];
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
            // Name match the <InjectionPoint>.
            int nameLen = GetInjectionPointLength( head );
            if( nameLen == 0 || !head.TryMatch( _injectionPoint.Name ) ) return false;
            head = head.TrimStart();
            // "revert"
            isRevert = head.TryMatch( "revert", StringComparison.OrdinalIgnoreCase );
            if( injectDef.Length > 0 )
            {
                // Capturing the opening tag this way suport any future "attribute". 
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

    internal static int GetInjectionPointLength( ReadOnlySpan<char> sHead )
    {
        int iS = 0;
        while( ++iS < sHead.Length && char.IsAsciiLetterOrDigit( sHead[iS] ) ) ;
        return iS;
    }
}
