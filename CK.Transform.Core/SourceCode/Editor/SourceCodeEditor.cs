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

    readonly TokenFilterBuilder _sharedBuilder;

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
        _sharedBuilder = new TokenFilterBuilder();
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
    public IScopedCodeEditor OpenScopedEditor() => _editor.OpenScoped();

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
