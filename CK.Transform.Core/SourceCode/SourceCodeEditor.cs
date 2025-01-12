using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static CK.Core.ActivityMonitor;

namespace CK.Transform.Core;

/// <summary>
/// Editor for <see cref="SourceCode"/>.
/// </summary>
[DebuggerDisplay( "{ToString(),nq}" )]
public sealed class SourceCodeEditor : IEnumerable<SourceToken>
{
    internal readonly SourceCode _code;
    readonly ImmutableList<Token>.Builder _tokens;
    IAnalyzer _analyzer;
    bool _needReparse;

    /// <summary>
    /// Initializes a new editor on a source code.
    /// </summary>
    /// <param name="analyzer">Analyzer required for <see cref="Reparse(IActivityMonitor)"/>.</param>
    /// <param name="code">The source code to edit.</param>
    public SourceCodeEditor( IAnalyzer analyzer, SourceCode code )
    {
        Throw.CheckNotNullArgument( analyzer );
        Throw.CheckNotNullArgument( code );
        _code = code;
        _analyzer = analyzer;
        _tokens = code.Tokens.ToBuilder();
    }

    /// <summary>
    /// Gets the spans.
    /// </summary>
    public ISourceSpanRoot Spans => _code._spans;

    /// <summary>
    /// Gets the tokens.
    /// </summary>
    public ImmutableList<Token> Tokens => _code.Tokens;

    /// <summary>
    /// Enumerates the scoped <see cref="SourceToken"/>.
    /// <para>
    /// Note that the enumerator MUST be disposed once done with it because it contains a <see cref="ImmutableList{T}.Enumerator"/>
    /// that must be disposed.
    /// </para>
    /// </summary>
    public IEnumerable<SourceToken> SourceTokens => this;

    IEnumerator<SourceToken> IEnumerable<SourceToken>.GetEnumerator() => CreateTokenSourceEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => CreateTokenSourceEnumerator();

    IEnumerator<SourceToken> CreateTokenSourceEnumerator()
    {
        var e = _code.SourceTokens.GetEnumerator();
        // Combine filters.
        return e;
    }


    /// <summary>
    /// Gets the analyzer that is used by <see cref="Reparse(IActivityMonitor)"/>.
    /// </summary>
    public IAnalyzer Analyzer => _analyzer;

    /// <summary>
    /// Unconditionally reparses the <see cref="SourceCode"/>.
    /// </summary>
    /// <param name="monitor">Required monitor.</param>
    /// <returns>True on success, false on error.</returns>
    public bool Reparse( IActivityMonitor monitor ) => DoReparse( monitor, null );

    /// <summary>
    /// Unconditionally reparses the <see cref="SourceCode"/> with a new <paramref name="newAnalyzer"/>
    /// and sets it as the current <see cref="Analyzer"/>.
    /// </summary>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="newAnalyzer">New analyzer that replaces <see cref="Analyzer"/>.</param>
    /// <returns>True on success, false on error.</returns>
    public bool Reparse( IActivityMonitor monitor, IAnalyzer newAnalyzer ) => DoReparse( monitor, newAnalyzer );

    bool DoReparse( IActivityMonitor monitor, IAnalyzer? newAnalyzer )
    {
        using( monitor.OpenTrace( "Parsing transformation result." ) )
        {
            if( newAnalyzer != null )
            {
                monitor.Trace( $"Changing language from '{_analyzer.LanguageName}' to '{newAnalyzer.LanguageName}'." );
                _analyzer = newAnalyzer;
            }
            string text = _code.ToString();
            var r = _analyzer.TryParse( monitor, text.AsMemory() );
            if( r == null ) return false;
            r.SourceCode.TransferTo( _code );
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
    /// Changes are not visible until <see cref="ApplyChanges"/> is called. The range (<paramref name="index"/> and <paramref name="count"/>)
    /// is relative to the current <see cref="SourceCode.Tokens"/>, independent of any previous replace, insert, remove not yet applied.
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
    /// <para>
    /// Changes are not visible until <see cref="ApplyChanges"/> is called. The <paramref name="index"/> is relative to the
    /// current <see cref="SourceCode.Tokens"/> and must not fall into any previously modified ranges not yet applied.
    /// </para>
    /// </summary>
    /// <param name="index">The index of the inserted tokens.</param>
    /// <param name="tokens">New tokens to insert. Must not be empty.</param>
    public void InsertAt( int index, params Token[] tokens )
    {
        Throw.CheckArgument( index >= 0 && index < _tokens.Count );
        Throw.CheckArgument( tokens.Length > 0 );
        DoReplace( index, 0, tokens );
    }

    /// <summary>
    /// Inserts new tokens. Spans that start at <paramref name="index"/> will not contain the inserted tokens.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="tokens"></param>
    public void InsertBefore( int index, params Token[] tokens )
    {
        Throw.CheckArgument( index >= 0 && index < _tokens.Count );
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
            _code._spans.OnRemoveTokens( index + delta, -delta );
        }
        _code.SetTokens( _tokens.ToImmutableList() );
    }

    /// <summary>
    /// Returns the source code.
    /// </summary>
    /// <returns>The source code.</returns>
    public override string ToString() => _code.ToString();
}
