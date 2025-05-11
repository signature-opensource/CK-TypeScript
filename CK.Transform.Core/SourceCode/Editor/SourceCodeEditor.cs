using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Editor for <see cref="SourceCode"/>.
/// </summary>
[DebuggerDisplay( "{ToString(),nq}" )]
public sealed partial class SourceCodeEditor
{
    readonly SourceCode _code;
    List<Token> _tokens;
    readonly Editor _editor;

    readonly SourceTokenEnumerable _sourceTokens;
    readonly List<SourceSpanTokenEnumerable> _enumerators;
    readonly List<DynamicSpans> _dynamicSpans;

    readonly IActivityMonitor _monitor;
    readonly ActivityMonitorExtension.ErrorTracker _errorTracker;
    bool _hasError;

    TransformerHost.Language _language;
    bool _needReparse;

    internal SourceCodeEditor( IActivityMonitor monitor,
                               TransformerHost.Language language,
                               SourceCode code )
    {
        Throw.CheckNotNullArgument( language );
        Throw.CheckNotNullArgument( code );
        _monitor = monitor;
        _language = language;
        _code = code;
        _tokens = code.InternalTokens;
        _errorTracker = monitor.OnError( OnError );
        _sourceTokens = new SourceTokenEnumerable( this );
        _enumerators = new List<SourceSpanTokenEnumerable>();
        _dynamicSpans = new List<DynamicSpans>();
        _editor = new Editor( this );
    }

    void OnError() => _hasError = true;

    internal void Dispose()
    {
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
    /// Gets the language.
    /// </summary>
    public TransformerHost.Language Language => _language;

    /// <summary>
    /// Pushes a new token filter.
    /// Returns the number of actual filters that have been pushed. This count must be provided to <see cref="PopTokenFilter"/>.
    /// </summary>
    /// <param name="filterProvider">The filter provider to apply.</param>
    /// <returns>The number of actual filters that have been pushed.</returns>
    public int PushTokenFilter( IFilteredTokenEnumerableProvider? filterProvider )
    {
        Throw.CheckState( _editor.OpenCount == 0 );
        Throw.DebugAssert( _editor.CurrentFilter.IsSyntaxBorder );
        int count = _editor.CurrentFilter.Index;
        filterProvider?.Activate( _editor.PushTokenFilter );
        count = _editor.CurrentFilter.Index - count;
        _editor.CurrentFilter.SetSyntaxBorder();
        return count;
    }

    /// <summary>
    /// Pops the last pushed token filters.
    /// </summary>
    /// <param name="count">
    /// Number of filters returned by <see cref="PushTokenFilter(IFilteredTokenEnumerableProvider)"/>.
    /// This can be 0.
    /// </param>
    public void PopTokenFilter( int count )
    {
        Throw.CheckArgument( count >= 0 );
        Throw.CheckState( _editor.OpenCount == 0 && _editor.CurrentFilter.Index >= count );
        Throw.DebugAssert( _editor.CurrentFilter.IsSyntaxBorder );
        _editor.PopTokenFilter( count );
        Throw.CheckState( _editor.CurrentFilter.IsSyntaxBorder );
    }

    /// <summary>
    /// Gets a disposable <see cref="Editor"/> that enables code modification.
    /// </summary>
    /// <returns>The editor that must be disposed.</returns>
    public Editor OpenEditor() => _editor.Open();

    /// <summary>
    /// Unconditionally reparses the <see cref="SourceCode"/>.
    /// </summary>
    /// <returns>True on success, false on error.</returns>
    public bool Reparse() => DoReparse( null );

    /// <summary>
    /// Unconditionally reparses the <see cref="SourceCode"/> with a new <paramref name="newAnalyzer"/>
    /// and sets it as the current <see cref="Analyzer"/>.
    /// </summary>
    /// <param name="newLanguage">New language that replaces <see cref="Language"/>.</param>
    /// <returns>True on success, false on error.</returns>
    public bool Reparse( TransformerHost.Language newLanguage ) => DoReparse( newLanguage );

    bool DoReparse( TransformerHost.Language? newLanguage )
    {
        using( _monitor.OpenTrace( "Parsing transformation result." ) )
        {
            if( newLanguage != null )
            {
                _monitor.Trace( $"Changing language from '{_language.LanguageName}' to '{newLanguage.LanguageName}'." );
                _language = newLanguage;
            }
            string text = _code.ToString();
            var r = _language.TargetLanguageAnalyzer.TryParse( _monitor, text.AsMemory() );
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
    /// Replaces the tokens starting at <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index of the first token that must be replaced.</param>
    /// <param name="tokens">Updated tokens. Must not be empty.</param>
    public void Replace( int index, params Token[] tokens ) => Replace( index, tokens.Length, tokens );

    /// <summary>
    /// Replaces one or more tokens with any number of tokens.
    /// </summary>
    /// <param name="index">The index of the first token that must be replaced.</param>
    /// <param name="count">The number of tokens to replace. Must be positive.</param>
    /// <param name="tokens">New tokens to insert. Must not be empty.</param>
    public void Replace( int index, int count, params Token[] tokens )
    {
        Throw.CheckArgument( index >= 0 && index + count <= _tokens.Count );
        Throw.CheckArgument( tokens.Length > 0 );
        DoReplace( index, count, tokens );
    }

    /// <summary>
    /// Inserts new tokens. Spans that start at <paramref name="index"/> will contain the inserted tokens.
    /// </summary>
    /// <param name="index">The index of the inserted tokens.</param>
    /// <param name="tokens">New tokens to insert. Must not be empty.</param>
    public void InsertAt( int index, params Token[] tokens )
    {
        Throw.CheckArgument( index >= 0 && index <= _tokens.Count );
        Throw.CheckArgument( tokens.Length > 0 );
        DoReplace( index, 0, tokens );
    }

    /// <summary>
    /// Inserts new tokens. Spans that start at <paramref name="index"/> will not contain the inserted tokens.
    /// </summary>
    /// <param name="index">The index of the inserted tokens.</param>
    /// <param name="tokens">New tokens to insert. Must not be empty.</param>
    public void InsertBefore( int index, params Token[] tokens )
    {
        Throw.CheckArgument( index >= 0 && index <= _tokens.Count );
        Throw.CheckArgument( tokens.Length > 0 );
        DoReplace( index, 0, tokens, insertBefore: true );
    }

    /// <summary>
    /// Removes a token at a specified index.
    /// </summary>
    /// <param name="index">The token index to remove.</param>
    public void RemoveAt( int index )
    {
        Throw.CheckArgument( index >= 0 && index < _tokens.Count );
        DoReplace( index, 1, Array.Empty<Token>() );
    }

    /// <summary>
    /// Removes a range of tokens.
    /// </summary>
    /// <param name="index">The index of the first token to remove.</param>
    /// <param name="count">The number of tokens to remove.</param>
    public void RemoveRange( int index, int count )
    {
        Throw.CheckArgument( index >= 0 && index + count <= _tokens.Count );
        DoReplace( index, count, Array.Empty<Token>() );
    }

    bool DoReplace( int index, int count, Token[] tokens, bool insertBefore = false )
    {
        Throw.DebugAssert( count > 0 );
        int eLimit = index + count - 1;
        int delta = tokens.Length - count;
        if( delta > 0 )
        {
            _sourceTokens.OnInsertTokens( eLimit, delta );
            foreach( var e in _enumerators )
            {
                e.OnInsertTokens( eLimit, delta );
            }
            foreach( var s in _dynamicSpans )
            {
                s.OnInsertTokens( index, delta, insertBefore, eLimit );
            }
            for( int i = 0; i < count; ++i )
            {
                _tokens[index + i] = tokens[i];
            }
            _tokens.InsertRange( index + count, tokens.Skip( count ) );
            _code._spans.OnInsertTokens( index, delta, insertBefore );
        }
        else if( delta < 0 )
        {
            delta = -delta;
            TokenSpan removedHead = new( index, index + delta );
            _sourceTokens.OnRemoveTokens( eLimit, delta );
            foreach( var e in _enumerators )
            {
                e.OnRemoveTokens( eLimit, delta );
            }
            foreach( var s in _dynamicSpans )
            {
                s.OnRemoveTokens( removedHead, eLimit );
            }
            for( int i = 0; i < tokens.Length; ++i )
            {
                _tokens[index + i] = tokens[i];
            }
            _tokens.RemoveRange( index + tokens.Length, delta );
            _code._spans.OnRemoveTokens( removedHead );
        }
        else
        {
            for( int i = 0; i < count; ++i )
            {
                _tokens[index + i] = tokens[i];
            }
        }
        _code.OnTokensChanged();
        return true;
    }

    internal void Track( DynamicSpans s ) => _dynamicSpans.Add( s );

    /// <summary>
    /// Returns the source code.
    /// </summary>
    /// <returns>The source code.</returns>
    public override string ToString() => _code.ToString();
}
