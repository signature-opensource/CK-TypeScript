using CK.Core;
using System;
using System.Collections.Immutable;

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
        // The finder will find the first match (or none) and will error on duplicate
        // or injection point. We need the same state machine for all the tokens and
        // process all the tokens (to detect duplicate errors).
        using var e = editor.OpenScopedEditor();
        bool noTokenAtAll = true;
        while( e.Tokens.NextEach(skipEmpty: false) )
        {
            noTokenAtAll = false;
            while( e.Tokens.NextMatch() )
            {
                var finder = new InjectionPointFinder( Target, Content );
                SourceToken modified = default;
                int tokenCount = 0;
                while( e.Tokens.NextToken() )
                {
                    ++tokenCount;
                    var sourceToken = e.Tokens.Token;
                    var token = sourceToken.Token;
                    var newTrivias = ProcessTrivias( monitor, ref finder, token.LeadingTrivias );
                    if( !newTrivias.IsDefault )
                    {
                        Throw.DebugAssert( !modified.IsValid );
                        modified = new SourceToken( token.SetTrivias( newTrivias ), sourceToken.Index );
                    }
                    newTrivias = ProcessTrivias( monitor, ref finder, token.TrailingTrivias );
                    if( !newTrivias.IsDefault )
                    {
                        Throw.DebugAssert( !modified.IsValid );
                        modified = new SourceToken( token.SetTrivias( token.LeadingTrivias, newTrivias ), sourceToken.Index );
                    }
                }
                if( !modified.IsValid )
                {
                    monitor.Error( $"Unable to find the injection point '{Target}' in {tokenCount} tokens." );
                }
                else
                {
                    e.Replace( modified.Index, modified.Token );
                    editor.SetNeedReparse();
                }
            }
        }
        if( noTokenAtAll )
        {
            monitor.Error( $"Empty scope token. Unable to find the injection point '{Target}'." );
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

