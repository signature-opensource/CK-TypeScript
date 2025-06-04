using CK.Core;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CK.Transform.Core;

/// <summary>
/// Editor for <see cref="SourceCode"/>.
/// </summary>
[DebuggerDisplay( "{ToString(),nq}" )]
public sealed partial class SourceCodeEditor : IDisposable
{
    readonly SourceCode _code;
    List<Token> _tokens;
    readonly Editor _editor;

    readonly TokenFilterBuilder _sharedBuilder;

    readonly IActivityMonitor _monitor;
    readonly ActivityMonitorExtension.ErrorTracker _errorTracker;
    bool _hasError;

    TransformerHost.Language? _language;
    IAnalyzer? _analyzer;
    bool _needReparse;

    /// <summary>
    /// Initializes a new editor. This is primarily used by <see cref="TransformerHost.Transform(IActivityMonitor, string, IEnumerable{TransformerFunction})"/>
    /// and is public only for tests.
    /// </summary>
    /// <param name="monitor">Captured monitor. Used internally to track errors.</param>
    /// <param name="code">The code to edit.</param>
    /// <param name="language">The language (required by <see cref="Reparse()"/>).</param>
    public SourceCodeEditor( IActivityMonitor monitor,
                             SourceCode code,
                             TransformerHost.Language language )
        : this( monitor, code, language?.TargetLanguageAnalyzer )
    {
        Throw.CheckNotNullArgument( language );
        _language = language;
    }

    /// <summary>
    /// Initializes a new editor, optionally bound to a <see cref="IAnalyzer"/>.
    /// </summary>
    /// <param name="monitor">Captured monitor. Used internally to track errors.</param>
    /// <param name="code">The code to edit.</param>
    /// <param name="analyzer">The analyzer (required by <see cref="Reparse()"/>).</param>
    public SourceCodeEditor( IActivityMonitor monitor,
                             SourceCode code,
                             IAnalyzer? analyzer = null )
    {
        Throw.CheckNotNullArgument( code );
        Throw.CheckState( !code.HasEditor );
        code.HasEditor = true;
        _monitor = monitor;
        _analyzer = analyzer;
        _code = code;
        _tokens = code.InternalTokens;
        _errorTracker = monitor.OnError( OnError );
        _sharedBuilder = new TokenFilterBuilder();
        _editor = new Editor( this );
    }

    void OnError() => _hasError = true;

    /// <summary>
    /// Release this editor.
    /// <see cref="Code"/> is free to be edited by another editor.
    /// </summary>
    public void Dispose()
    {
        _code.HasEditor = false;
        _errorTracker.Dispose();
    }

    /// <summary>
    /// Gets the monitor that must be used to signal errors.
    /// </summary>
    public IActivityMonitor Monitor => _monitor;

    /// <summary>
    /// Gets whether this editor is on error.
    /// </summary>
    public bool HasError => _hasError;

    /// <summary>
    /// Gets the edited code.
    /// </summary>
    public SourceCode Code => _code;

    /// <summary>
    /// Gets the language if it has been specified.
    /// </summary>
    public TransformerHost.Language? Language => _language;

    /// <summary>
    /// Gets the analyzer if it has been specified.
    /// </summary>
    public IAnalyzer? Analyzer => _analyzer;

/// <summary>
    /// Pushes a new token operator.
    /// Returns the number of actual operators that have been pushed. This count must be provided to <see cref="PopTokenOperator"/>.
    /// </summary>
    /// <param name="tokenOperator">The operator to apply.</param>
    /// <returns>The number of actual operators that have been pushed.</returns>
    public int PushTokenOperator( ITokenFilterOperator? tokenOperator )
    {
        Throw.CheckState( _editor.OpenState is OpenEditorState.None );
        Throw.DebugAssert( _editor.CurrentFilter.IsSyntaxBorder );
        if( tokenOperator == null || tokenOperator == ITokenFilterOperator.Empty )
        {
            return 0;
        }
        int count = _editor.CurrentFilter.Index;
        tokenOperator?.Activate( _editor.PushTokenOperator );
        count = _editor.CurrentFilter.Index - count;
        _editor.CurrentFilter.SetSyntaxBorder();
        return count;
    }

    /// <summary>
    /// Pops the last pushed token operators.
    /// </summary>
    /// <param name="count">
    /// Number of operators returned by <see cref="PushTokenOperator(ITokenFilterOperator)"/>.
    /// This can be 0.
    /// </param>
    public void PopTokenOperator( int count )
    {
        Throw.CheckArgument( count >= 0 );
        Throw.CheckState( _editor.OpenState is OpenEditorState.None && _editor.CurrentFilter.Index >= count );
        Throw.DebugAssert( _editor.CurrentFilter.IsSyntaxBorder );
        if( count > 0 )
        {
            _editor.PopTokenOperator( count );
            Throw.CheckState( _editor.CurrentFilter.IsSyntaxBorder );
        }
    }

    /// <summary>
    /// Gets a disposable <see cref="ICodeEditor"/> that enables global code modification.
    /// </summary>
    /// <returns>The editor that must be disposed.</returns>
    public ICodeEditor OpenGlobalEditor() => _editor.OpenGlobal();

    /// <summary>
    /// Gets a disposable <see cref="IScopedCodeEditor"/> that supports code
    /// modification on the filtered tokens.
    /// </summary>
    /// <returns>The editor that must be disposed.</returns>
    public IScopedCodeEditor OpenScopedEditor( bool allowEmpty = false ) => _editor.OpenScoped( allowEmpty );

    /// <summary>
    /// Unconditionally reparses the <see cref="SourceCode"/>.
    /// </summary>
    /// <returns>True on success, false on error.</returns>
    public bool Reparse() => DoReparse( null );

    /// <summary>
    /// Unconditionally reparses the <see cref="SourceCode"/> with a new <paramref name="newLanguage"/>
    /// and sets it as the current <see cref="Language"/> (and <see cref="Analyzer"/>).
    /// </summary>
    /// <param name="newLanguage">New language that replaces <see cref="Language"/>.</param>
    /// <returns>True on success, false on error.</returns>
    public bool Reparse( TransformerHost.Language newLanguage ) => DoReparse( newLanguage );

    bool DoReparse( TransformerHost.Language? newLanguage )
    {
        Throw.CheckArgument( newLanguage != null || Analyzer != null );
        using( _monitor.OpenTrace( "Parsing transformation result." ) )
        {
            if( newLanguage != null )
            {
                if( _language != null )
                {
                    _monitor.Trace( $"Changing language from '{_language.LanguageName}' to '{newLanguage.LanguageName}'." );
                }
                _language = newLanguage;
            }
            Throw.DebugAssert( _analyzer != null );
            string text = _code.ToString();
            var r = _analyzer.TryParse( _monitor, text.AsMemory() );
            if( r == null ) return false;
            r.SourceCode.TransferTo( _code );
            _tokens = _code.InternalTokens;
            _needReparse = false;
            return true;
        }
    }

    /// <summary>
    /// Gets whether <see cref="Reparse(IActivityMonitor, IAnalyzer?)"/> must be called.
    /// </summary>
    public bool NeedReparse => _needReparse;

    /// <summary>
    /// Sets <see cref="NeedReparse"/> to true.
    /// </summary>
    public void SetNeedReparse() => _needReparse = true;

    /// <summary>
    /// Adds a span. This throws if the span is attached to a root or
    /// if it intersects an existing span.
    /// </summary>
    /// <param name="newOne">The span to add.</param>
    public void AddSourceSpan( SourceSpan newOne ) => _code._spans.Add( newOne );

    /// <summary>
    /// Removes a source span with its tokens. The <paramref name="span"/>
    /// is detached using <see cref="SourceSpan.DetachMode.KeepChildren"/>.
    /// </summary>
    /// <param name="span">The span to remove from the code.</param>
    public void RemoveSpan( SourceSpan span )
    {
        Throw.CheckArgument( !span.IsDetached && span.GetRoot() == _code.Spans );
        // Removes the tokens.
        _tokens.RemoveRange( span.Span.Beg, span.Span.Length );
        // Detaches the span and its children.
        span.Detach( SourceSpan.DetachMode.KeepChildren );
        // Adjusts, the other spans positions.
        _code._spans.OnRemoveTokens( span.Span );
    }

    /// <summary>
    /// Moves a source span and its token before another one.
    /// </summary>
    /// <param name="span">The span to move.</param>
    /// <param name="newNext">The span before which <paramref name="span"/> must be moved.</param>
    public void MoveSpanBefore( SourceSpan span, SourceSpan newNext )
    {
        Throw.CheckArgument( span != newNext
                             && !span.IsDetached && !newNext.IsDetached
                             && span.GetRoot() == _code.Spans
                             && newNext.GetRoot() == _code.Spans );

        var rel = newNext.Span.GetRelationship( span.Span );
        if( rel is SpanRelationship.Independent or SpanRelationship.Contiguous )
        {
            var tokens = CollectionsMarshal.AsSpan( _tokens );
            var moved = tokens.Slice( span.Span.Beg, span.Span.Length ).ToArray();
            tokens.Slice( newNext.Span.Beg, span.Span.Beg - newNext.Span.Beg )
                .CopyTo( tokens.Slice( newNext.Span.Beg + span.Span.Length ) );
            moved.CopyTo( tokens.Slice( newNext.Span.Beg ) );
            _code._spans.MoveSpanBefore( span, newNext );
            _code.OnTokensChanged();
        }
        else
        {
            Throw.InvalidOperationException( $"Spans '{span.Span}' and '{newNext.Span}' are already ordered or overlap." );
        }
    }

    bool DoReplace( int index, int count, ReadOnlySpan<Token> tokens, bool insertBefore = false )
    {
        Throw.DebugAssert( count >= 0 );
        // All existing Enumerators must have observed the
        // last replaced token.
        int eLimit = index + count - 1;
        Throw.CheckArgument( "Invalid token range to replace.", index >= 0 && eLimit < _tokens.Count );
        // To limit memory moves, tokens are replaced in the list:
        // RemoveRange or AddRange is only called when required.
        int delta = tokens.Length - count;
        if( delta > 0 )
        {
            for( int i = 0; i < count; ++i )
            {
                _tokens[index + i] = tokens[i];
            }
            _tokens.InsertRange( index + count, tokens.Slice( count ) );
            _code._spans.OnInsertTokens( index, delta, insertBefore );
        }
        else if( delta < 0 )
        {
            var positiveDelta = -delta;
            TokenSpan removedHead = new( index, index + positiveDelta );
            for( int i = 0; i < tokens.Length; ++i )
            {
                _tokens[index + i] = tokens[i];
            }
            _tokens.RemoveRange( index + tokens.Length, positiveDelta );
            _code._spans.OnRemoveTokens( removedHead );
        }
        else
        {
            for( int i = 0; i < tokens.Length; ++i )
            {
                _tokens[index + i] = tokens[i];
            }
        }
        _editor.OnUpdateTokens( eLimit, delta );
        _code.OnTokensChanged();
        return true;
    }

    /// <summary>
    /// Returns the source code.
    /// </summary>
    /// <returns>The source code.</returns>
    public override string ToString() => _code.ToString();

}
