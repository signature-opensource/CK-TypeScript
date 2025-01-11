using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using static CK.Core.ActivityMonitor;

namespace CK.Transform.Core;

/// <summary>
/// Editor for <see cref="SourceCode"/>.
/// </summary>
public sealed class SourceCodeEditor : IEnumerable<SourceToken>
{
    readonly SourceCode _code;
    readonly ImmutableList<Token>.Builder _tokens;
    IAnalyzer _analyzer;
    bool _needReparse;

    // Cannot use a TokenSpan here since Count is 0 for an insertion.
    readonly record struct Mod( int Index, int Count, Token[] Tokens, bool InsertBefore )
    {
        public int Delta => Tokens.Length - Count;
    }
    readonly List<Mod> _mods;

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
        _mods = new List<Mod>();
        _tokens = code.Tokens.ToBuilder();
    }

    /// <summary>
    /// Gets the source code that is edited.
    /// </summary>
    public SourceCode SourceCode => _code;

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
    /// Gets whether changes are pending.
    /// </summary>
    public bool HasPendingChanges => _mods.Count > 0;

    /// <summary>
    /// Applies all the change at once.
    /// <see cref="SourceCode"/> is updated: <see cref="SourceCode.Tokens"/> and <see cref="SourceCode.Spans"/>.
    /// <para>
    /// If no pending changes exist, this does nothing.
    /// </para>
    /// </summary>
    public ImmutableList<Token> ApplyChanges()
    {
        if( _mods.Count ==  0 ) return _code.Tokens;
        int offset = 0;
        foreach( var m in CollectionsMarshal.AsSpan( _mods ) )
        {
            int oIdx = m.Index + offset;
            if( m.Count > 0 )
            {
                Throw.DebugAssert( m.InsertBefore is false );
                _tokens.RemoveRange( oIdx, m.Count );
            }
            if( m.Tokens.Length > 0 )
            {
                _tokens.InsertRange( oIdx, m.Tokens );
            }
            int delta = m.Delta;
            if( delta > 0 )
            {
                _code._spans.OnInsertTokens( oIdx, delta, m.InsertBefore );
            }
            else if( delta < 0 )
            {
                Throw.DebugAssert( m.InsertBefore is false );
                _code._spans.OnRemoveTokens( oIdx + delta, -delta );
            }

            offset += delta;
        }
        _mods.Clear();
        var newTokens = _tokens.ToImmutableList();
        _code.SetTokens( newTokens );
        return newTokens;
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
    /// <remarks>
    /// The new span MUST not be updated by subsequent ApplyChanges.
    /// We first call <see cref="ApplyChanges()"/>.
    /// <para>
    /// To avoid this the immutable road seems to be a dead end as it will
    /// require the SourceSpan to support deep cloning or to be immutable.
    /// <para>
    /// (Deep cloning is not a good idea.)
    /// </para>
    /// Having immutable SourceSpan can be done IF the begin...end
    /// "transaction" appears to be crucial.
    /// SourceSpans are conceptually in a tree and, first, additions are
    /// by design made from the root and, second, a mutation visitor
    /// with a Replace( Func&lt;SourceSpan,SourceSpan?&gt; ) would do the job.
    /// <para>
    /// Unfortunately, adding a token at the start, implies the recreation
    /// of the whole tree (to offset the spans).
    /// Making the only the Span falsely immutable?
    /// This would require 2 spans (_committed and _editing) and exposing them
    /// (odd!) or accessing them through a codeSource.GetSpan( SourceSpan s ) and
    /// a editor.GetSpan( SourceSpan s ) that would return the right one.
    /// This could be a way...
    /// </para>
    /// For the moment, we keep the mutable SourceSpan. Thanks to the ISourceSpanRoot,
    /// and this AddSourceSpan (on the editor) the API is compatible whith an
    /// immutable implementation.
    /// </para>
    /// </remarks>
    /// <param name="newOne">The span to add.</param>
    public void AddSourceSpan( SourceSpan newOne )
    {

        if( HasPendingChanges ) ApplyChanges();
        _code._spans.Add( newOne );
    }

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

    void DoReplace( int index, int count, Token[] tokens )
    {
        var d = new TokenSpan( index, index + count );
        int idx = 0;
        var mods = CollectionsMarshal.AsSpan( _mods );
        foreach( var m in mods )
        {
            if( m.Count > 0 )
            {
                TokenSpan mSpan = new TokenSpan( m.Index, m.Index + m.Count );
                var r = d.GetRelationship( mSpan );
                if( r is SpanRelationship.Equal && m.Delta == 0 && tokens.Length == count )
                {
                    // The exact same range of tokens has previously been overwritten and we are overwriting it again.
                    // We consider that the new overwrite is an addition.
                    // We may extend this semantics to the SpanRelationShips.SameStart and even allow any replaced tokens' length...
                    // For the moment, we keep this "minimal" allowed combination.
                    mods[idx] = new Mod( m.Index, m.Count, m.Tokens.Concat( tokens ).ToArray(), false );
                    return;
                }
                if( (r & ~SpanRelationship.Swapped) is not SpanRelationship.Independent and not SpanRelationship.Contiguous )
                {
                    Throw.InvalidOperationException( $"Span '{d}' intersects an already modified token span '{mSpan}'." );
                }
                // d is before m.OriginalSpan: we found the insertion point.
                if( (r & SpanRelationship.Swapped) == 0 ) break;
            }
            else
            {
                // m is a pure insertion.
                if( d.End < m.Index )
                {
                    // d is before the insertion point: we found the insertion point.
                    break;
                }
                // if the replaced range d contains the insertion m then its bad...
                // ...except if d starts exactly at m and is an InsertBefore: we can
                // both insert before AND the replaced range d.
                if( d.Contains( m.Index ) && !(d.Beg == m.Index && m.InsertBefore) )
                {
                    // We are replacing a span of tokens in which some tokens have been inserted.
                    // It is hard to find a semantically correct semantics for this.
                    Throw.InvalidOperationException( $"Span '{d}' contains {m.Tokens.Length} previously inserted tokens." );
                }
            }
            ++idx;
        }
        _mods.Insert( idx, new Mod( index, count, tokens, false ) );
    }

    /// <summary>
    /// Immediately overwrites the given number of tokens starting at <paramref name="index"/>.
    /// This shortcuts <see cref="ApplyChanges()"/>, <see cref="SourceCode.Tokens"/> is updated
    /// but the effect is not observable on existing enumerators: new enumerators must be obtained
    /// for this change to be visible.
    /// </summary>
    /// <param name="index">The index of the first token that must be replaced.</param>
    /// <param name="tokens">Updated tokens. Must not be empty.</param>
    public void InPlaceReplace( int index, params Token[] tokens )
    {
        Throw.CheckArgument( index >= 0 && index + tokens.Length <= _tokens.Count );
        Throw.CheckArgument( tokens.Length > 0 );
        _tokens.RemoveRange( index, tokens.Length );
        _tokens.InsertRange( index, tokens );
        _code.SetTokens( _tokens.ToImmutableList() );
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
        int idx = 0;
        var mods = CollectionsMarshal.AsSpan( _mods );
        foreach( var m in mods )
        {
            if( index > m.Index || index == m.Index && !m.InsertBefore )
            {
                if( index == m.Index )
                {
                    Throw.DebugAssert( m.InsertBefore is false );
                    mods[idx] = new Mod( m.Index, m.Count, tokens.Concat( m.Tokens ).ToArray(), m.InsertBefore );
                    return;
                }
                if( m.Count > 0 )
                {
                    int mEnd = m.Index + m.Count;
                    if( index <= mEnd )
                    {
                        if( index == mEnd )
                        {
                            mods[idx] = new Mod( m.Index, m.Count, m.Tokens.Concat( tokens ).ToArray(), false );
                            return;
                        }
                        Throw.InvalidOperationException( $"Inserting at '{index}' is inside an already modified token span '[{m.Index},{mEnd}['." );
                    }
                }
                // index is after m: continue the loop.
            }
            else
            {
                // index is before m: we found the insertion point.
                break;
            }
            ++idx;
        }
        _mods.Insert( idx, new Mod( index, 0, tokens, false ) );
    }

    /// <summary>
    /// Inserts new tokens. Spans that start at <paramref name="index"/> will not contain the inserted tokens.
    /// <para>
    /// Changes are not visible until <see cref="ApplyChanges"/> is called. The <paramref name="index"/> is relative to the
    /// current <see cref="SourceCode.Tokens"/> and must not fall into any previously modified ranges not yet applied.
    /// </para>
    /// </summary>
    /// <param name="index"></param>
    /// <param name="tokens"></param>
    public void InsertBefore( int index, params Token[] tokens )
    {
        Throw.CheckArgument( index >= 0 && index < _tokens.Count );
        Throw.CheckArgument( tokens.Length > 0 );
        int idx = 0;
        var mods = CollectionsMarshal.AsSpan( _mods );
        foreach( var m in mods )
        {
            if( index >= m.Index )
            {
                if( index == m.Index )
                {
                    if( m.InsertBefore )
                    {
                        mods[idx] = new Mod( m.Index, m.Count, tokens.Concat( m.Tokens ).ToArray(), true );
                        return;
                    }
                    // m is a regular insert (after).
                    // This InsertBefore must be before.
                    break;
                }
                if( m.Count > 0 )
                {
                    int mEnd = m.Index + m.Count;
                    if( index <= mEnd )
                    {
                        if( index == mEnd )
                        {
                            mods[idx] = new Mod( m.Index, m.Count, m.Tokens.Concat( tokens ).ToArray(), true );
                            return;
                        }
                        Throw.InvalidOperationException( $"Inserting at '{index}' is inside an already modified token span '[{m.Index},{mEnd}['." );
                    }
                }
                // index is after m: continue the loop.
            }
            else
            {
                // index is before m: we found the insertion point.
                break;
            }
            ++idx;
        }
        _mods.Insert( idx, new Mod( index, 0, tokens, true ) );
    }

    /// <summary>
    /// Removes a token at a specified index.
    /// <para>
    /// Changes are not visible until <see cref="ApplyChanges"/> is called. The <paramref name="index"/> is relative to the
    /// current <see cref="SourceCode.Tokens"/> and must not fall into any previously modified ranges not yet applied.
    /// </para>
    /// </summary>
    /// <param name="index">The token index to remove.</param>
    public void RemoveAt( int index )
    {
        Throw.CheckArgument( index >= 0 && index < _tokens.Count );
        DoReplace( index, 1, Array.Empty<Token>() );
    }

    /// <summary>
    /// Removes a range of tokens.
    /// <para>
    /// Changes are not visible until <see cref="ApplyChanges"/> is called. The range (<paramref name="index"/> and <paramref name="count"/>)
    /// is relative to the current <see cref="SourceCode.Tokens"/>, independent of any previous replace, insert, remove not yet applied.
    /// The range must not intersect any previously modified ranges not yet applied.
    /// </para>
    /// </summary>
    /// <param name="index">The index of the first token to remove.</param>
    /// <param name="count">The number of tokens to remove.</param>
    public void RemoveRange( int index, int count )
    {
        Throw.CheckArgument( index >= 0 && index + count <= _tokens.Count );
        Replace( index, count, Array.Empty<Token>() );
    }
}
