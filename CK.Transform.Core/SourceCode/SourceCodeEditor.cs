using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace CK.Transform.Core;

/// <summary>
/// Editor for <see cref="SourceCode"/>.
/// </summary>
[DebuggerDisplay( "{ToString(),nq}" )]
public sealed partial class SourceCodeEditor
{
    readonly SourceCode _code;
    List<Token> _tokens;
    readonly TokenScope _scopedTokens;

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
        _scopedTokens = new TokenScope( this );
        _errorTracker = monitor.OnError( OnError );
    }

    void OnError() => _hasError = true;

    internal void InternalDispose() => _errorTracker.Dispose();

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
    /// Gets the filtered <see cref="SourceToken"/>.
    /// <para>
    /// Note that the enumerator MUST be disposed once done with it because it
    /// contains a <see cref="ImmutableList{T}.Enumerator"/> that must be disposed.
    /// </para>
    /// </summary>
    public TokenScope ScopedTokens => _scopedTokens;

    /// <summary>
    /// Gets the language.
    /// </summary>
    public TransformerHost.Language Language => _language;

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
            var r = _language.TargetAnalyzer.TryParse( _monitor, text.AsMemory() );
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
    /// if it intersects an existing existing span.
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
    /// <para>
    /// The range (<paramref name="index"/> and <paramref name="count"/>) is relative to the current <see cref="SourceCode.Tokens"/>,
    /// independent of any previous replace, insert, remove not yet applied.
    /// The range must not intersect any previously modified ranges not yet applied.
    /// </para>
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

    void DoReplace( int index, int count, Token[] tokens, bool insertBefore = false )
    {
        if( count > 0 ) _tokens.RemoveRange( index, count );
        if( tokens.Length > 0 )
        {
            _tokens.InsertRange( index, tokens );
        }
        int delta = tokens.Length - count;
        if( delta > 0 )
        {
            _code._spans.OnInsertTokens( index, delta, insertBefore );
        }
        else if( delta < 0 )
        {
            _code._spans.OnRemoveTokens( index, -delta );
        }
        _code.OnTokensChanged();
    }

    /// <summary>
    /// Returns the source code.
    /// </summary>
    /// <returns>The source code.</returns>
    public override string ToString() => _code.ToString();
}
