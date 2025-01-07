using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// Editor for <see cref="SourceCode"/>.
/// </summary>
public sealed class SourceCodeEditor
{
    readonly SourceCode _code;
    readonly ImmutableList<Token>.Builder _tokens;
    IAnalyzer _analyzer;
    bool _needReparse;

    // Cannot use a TokenSpan here since Count is 0 for an insertion.
    readonly record struct Mod( int Index, int Count, params Token[] Tokens )
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
            string text = _code.Tokens.Write( new StringBuilder() ).ToString();
            var r = _analyzer.SafeParse( monitor, text.AsMemory() );
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
    /// Creates a <see cref="ISourceTokenEnumerator"/> on all <see cref="Tokens"/>.
    /// </summary>
    /// <returns>An enumerator with index, token and spans covering the token.</returns>
    public ISourceTokenEnumerator CreateSourceTokenEnumerator()
    {
        var e = _code.CreateSourceTokenEnumerator();
        // Combine filters.
        return e;
    }

    /// <summary>
    /// Applies all the change at once. <see cref="SourceCode"/> is updated.
    /// If <see cref="NeedReparse"/> is true, this calls <see cref="Reparse(IActivityMonitor)"/>.
    /// </summary>
    public void Apply( IActivityMonitor monitor )
    {
        int offset = 0;
        foreach( var m in CollectionsMarshal.AsSpan( _mods ) )
        {
            int oIdx = m.Index + offset;
            if( m.Count > 0 )
            {
                _tokens.RemoveRange( oIdx, m.Count );
            }
            if( m.Tokens.Length > 0 )
            {
                _tokens.InsertRange( oIdx, m.Tokens );
            }
            int delta = m.Delta;
            if( delta > 0 ) _code.Spans.OnAddTokens( oIdx, delta );
            else if( delta < 0 ) _code.Spans.OnRemoveTokens( oIdx + delta, -delta );
            offset += m.Delta;
        }
        _mods.Clear();
        _code.SetTokens( _tokens.ToImmutableList() );
        if( _needReparse ) Reparse( monitor );
    }

    /// <summary>
    /// Replaces one or more tokens with any number of tokens.
    /// <para>
    /// Changes are not visible until <see cref="Apply"/> is called. The range (<paramref name="index"/> and <paramref name="count"/>)
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
        var d = new TokenSpan( index, index + count );
        int idx = 0;
        foreach( var m in CollectionsMarshal.AsSpan( _mods ) )
        {
            if( m.Count > 0 )
            {
                TokenSpan mSpan = new TokenSpan( m.Index, m.Index + m.Count );
                var r = d.GetRelationship( mSpan );
                if( (r & ~SpanRelationship.Swapped) is not SpanRelationship.Independent and not SpanRelationship.Contiguous )
                {
                    Throw.InvalidOperationException( $"Span '{d}' intersects an already modified token span '{mSpan}'." );
                }
                // d is before m.OriginalSpan: we found the insertion point.
                if( (r & SpanRelationship.Swapped) == 0 ) break;
            }
            ++idx;
        }
        _mods.Insert( idx, new Mod( index, count, tokens ) );
    }

    /// <summary>
    /// Inserts new tokens.
    /// <para>
    /// Changes are not visible until <see cref="Apply"/> is called. The <paramref name="index"/> is relative to the
    /// current <see cref="SourceCode.Tokens"/> and must not fall into any previously modified ranges not yet applied.
    /// </para>
    /// </summary>
    /// <param name="index">The index of the inserted tokens.</param>
    /// <param name="tokens">New tokens to insert. Must not be empty.</param>
    public void Insert( int index, params Token[] tokens )
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
                    mods[idx] = new Mod( m.Index, m.Count, tokens.Concat( m.Tokens ).ToArray() );
                    return;
                }
                if( m.Count > 0 )
                {
                    int mEnd = m.Index + m.Count;
                    if( index <= mEnd )
                    {
                        if( index == mEnd )
                        {
                            mods[idx] = new Mod( m.Index, m.Count, m.Tokens.Concat( tokens ).ToArray() );
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
        _mods.Insert( idx, new Mod( index, 0, tokens ) );
    }

    /// <summary>
    /// Removes a token at a specified index.
    /// <para>
    /// Changes are not visible until <see cref="Apply"/> is called. The <paramref name="index"/> is relative to the
    /// current <see cref="SourceCode.Tokens"/> and must not fall into any previously modified ranges not yet applied.
    /// </para>
    /// </summary>
    /// <param name="index">The token index to remove.</param>
    public void RemoveAt( int index ) => Replace( index, 1 );

    /// <summary>
    /// Removes a range of tokens.
    /// <para>
    /// Changes are not visible until <see cref="Apply"/> is called. The range (<paramref name="index"/> and <paramref name="count"/>)
    /// is relative to the current <see cref="SourceCode.Tokens"/>, independent of any previous replace, insert, remove not yet applied.
    /// The range must not intersect any previously modified ranges not yet applied.
    /// </para>
    /// </summary>
    /// <param name="index">The index of the first token to remove.</param>
    /// <param name="count">The number of tokens to remove.</param>
    public void RemoveRange( int index, int count ) => Replace( index, count );
}
