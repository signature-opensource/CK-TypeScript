using CK.Core;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

/// <summary>
/// Inject into &lt;InsertionPoint/&gt; statement. 
/// </summary>
public sealed class InjectIntoStatement : TransformStatement
{
    RawString _content;
    InjectionPoint _target;

    /// <summary>
    /// Initializes a new <see cref="InjectIntoStatement"/>.
    /// </summary>
    /// <param name="beg">The start of the span. Must be greater or equal to 0.</param>
    /// <param name="end">The end of the span. Must be greater than <paramref name="beg"/>.</param>
    /// <param name="content">The content to inject.</param>
    /// <param name="target">The target injection point.</param>
    public InjectIntoStatement( int beg, int end, RawString content, InjectionPoint target )
        : base( beg, end )
    {
        _content = content;
        _target = target;
    }

    /// <summary>
    /// Gets the content to inject.
    /// </summary>
    public RawString Content => _content;

    /// <summary>
    /// Gets the target injection point.
    /// </summary>
    public InjectionPoint Target => _target;

    /// <inheritdoc />
    public override void Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        SourceToken modified = default;

        // The finder will find the first match (or none) and will error on duplicate
        // or injection point. We need the same state machine for all the tokens and
        // process all the tokens (to detect duplicate errors).
        var finder = new InjectionPointFinder( Target, Content );
        foreach( var sourceToken in editor.ScopedTokens.AllTokens )
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
            editor.Replace( modified.Index, modified.Token );
            editor.SetNeedReparse();
        }
    }

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
                Throw.DebugAssert( "Match only once.", modified.IsDefault );
                var colOffset = t.IsLineComment && finder.OpeningColumnPrefix.Length > 0
                                    ? new Trivia( TokenType.Whitespace, finder.OpeningColumnPrefix, checkContent: false )
                                    : default;
                if( !finder.InjectOpening.IsDefault )
                {
                    int len = trivias.Length + 2;
                    if( !colOffset.IsDefault )
                    {
                        len += idx == 0 ? 3 : 2;
                    }
                    var builder = ImmutableArray.CreateBuilder<Trivia>( len );
                    builder.AddRange( trivias, idx );
                    if( !colOffset.IsDefault && idx == 0 ) builder.Add( colOffset );
                    builder.Add( finder.InjectOpening );
                    if( !colOffset.IsDefault ) builder.Add( colOffset );
                    builder.Add( finder.InjectText );
                    if( !colOffset.IsDefault ) builder.Add( colOffset );
                    builder.Add( finder.InjectClosing );
                    var remaining = trivias.Length - idx - 1;
                    if( remaining > 0 )
                    {
                        builder.AddRange( trivias.AsSpan( idx + 1, remaining ) );
                    }
                    Throw.DebugAssert( builder.Capacity == builder.Count );
                    modified = builder.DrainToImmutable();
                }
                else
                {
                    int len = trivias.Length + (colOffset.IsDefault ? 1 : 2);
                    var builder = ImmutableArray.CreateBuilder<Trivia>( len );

                    // When isRevert is true: insert after the opening trivia (for line comment, it ends with a newline).
                    // When isRevert is false: insert before the closing trivia.
                    int insertIdx = finder.IsRevert ? idx + 1 : idx;
                    builder.AddRange( trivias, insertIdx );
                    if( finder.IsRevert )
                    {
                        // Insert the colOffset before the text.
                        if( !colOffset.IsDefault ) builder.Add( colOffset );
                        builder.Add( finder.InjectText );
                    }
                    else
                    {
                        // Inserting before the closing trivia: the text will "reuse" the existing
                        // offset of the closing trivia (whatever it is but it should be the same
                        // trivia as the initial opening tag unless the text has been deliberately tampered),
                        // and the colOffset becomes the trivia before the closing trivia. 
                        builder.Add( finder.InjectText );
                        if( !colOffset.IsDefault ) builder.Add( colOffset );
                    }
                    var remaining = trivias.Length - insertIdx;
                    if( remaining > 0 )
                    {
                        builder.AddRange( trivias.AsSpan( insertIdx, remaining ) );
                    }
                    Throw.DebugAssert( builder.Capacity == builder.Count );
                    modified = builder.DrainToImmutable();
                }
            }
            ++idx;
        }
        return modified;
    }

    // This is a ref struct only to emphasize the fact that this is
    // simply an encapsulation of a state machine.
    ref struct InjectionPointFinder
    {
        readonly InjectionPoint _injectionPoint;
        readonly RawString _injectedCode;
        string? _openingColumnPrefix;
        Trivia _injectOpening;
        Trivia _injectText;
        Trivia _injectClosing;
        bool _foundInsertPoint;
        bool _foundOpeningIsAutoClosing;
        bool _foundOpeningIsRevert;

        public InjectionPointFinder( InjectionPoint injectionPoint, RawString injectedCode )
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
        [MemberNotNullWhen(true, nameof(OpeningColumnPrefix) )]
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
                        var text = String.Join( Environment.NewLine + OpeningColumnPrefix, _injectedCode.Lines );
                        // Block comment injection is "inline".
                        if( t.IsLineComment ) text += Environment.NewLine;
                        _injectText = new Trivia( TokenType.Whitespace, text );
                        if( isAutoClosing )
                        {
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

        readonly bool Parse( ReadOnlySpan<char> sTrivia,
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
            int nameLen = InjectionPoint.GetInjectionPointLength( head );
            if( nameLen == 0 || !head.TryMatch( _injectionPoint.Name ) ) return false;
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

    internal static InjectIntoStatement? Parse( ref TokenizerHead head, Token inject )
    {
        Throw.DebugAssert( inject.Text.Span.Equals( "inject", StringComparison.Ordinal ) );
        int startStatement = head.LastTokenIndex;
        var content = RawString.Match( ref head );
        head.MatchToken( "into" );
        var target = InjectionPoint.Match( ref head );
        head.TryAcceptToken( TokenType.SemiColon, out _ );
        return content != null && target != null
                ? head.AddSpan( new InjectIntoStatement( startStatement, head.LastTokenIndex + 1, content, target ) )
                : null;
    }

}
