using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{
    /// <summary>
    /// A Dynamic span is defined by multiple non overlapping <see cref="TokenSpan"/> that
    /// can be considered as a each/range/token enumeration (see <see cref="GetEachGroups"/>)
    /// or as ranges of <see cref="SourceToken"/> (see <see cref="GetRanges"/>).
    /// <para>
    /// This acts as builder until either <see cref="LockEachGroups"/> or <see cref="LockRanges"/> is called.
    /// </para>
    /// <para>
    /// This can be created only by <see cref="IFilteredTokenEnumerableProvider.CreateDynamicSpan"/> method.
    /// </para>
    /// </summary>
    public sealed partial class DynamicSpans
    {
        readonly SourceCodeEditor _editor;
        readonly List<Range> _spans;
        IEnumerable<IEnumerable<IEnumerable<SourceToken>>>? _eachGroups;
        IEnumerable<IEnumerable<SourceToken>>? _ranges;

        internal DynamicSpans( SourceCodeEditor editor )
        {
            _editor = editor;
            _spans = new List<Range>();
        }

        /// <summary>
        /// Gets whether no new span can be added: <see cref="LockEachGroups"/> or <see cref="LockRanges"/>
        /// has been called.
        /// </summary>
        public bool IsLocked => _eachGroups != null || _ranges != null;

        /// <summary>
        /// Gets whether these spans must be considered as ranges of tokens. See <see cref="GetRanges"/>.
        /// </summary>
        public bool IsLockedAsRanges => _ranges != null;

        /// <summary>
        /// Gets whether these spans must be considered as a each/range/token enumeration. See <see cref="GetEachGroups"/>.
        /// </summary>
        [MemberNotNullWhen( true, nameof( _eachGroups ) )]
        public bool IsLockedAsEachGroups => _eachGroups != null;

        /// <summary>
        /// Locks these <see cref="Spans"/> as ranges of <see cref="SourceToken"/>. See <see cref="GetRanges"/>.
        /// <para>
        /// This must not already be locked and, if the <see cref="Spans"/> is not empty, the last
        /// <see cref="TokenSpan.End"/> must not be greater than <see cref="SourceCode.Tokens"/>'s count
        /// otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </para>
        /// </summary>
        /// <returns>These Spans as ranges of <see cref="SourceToken"/>.</returns>
        public IEnumerable<IEnumerable<SourceToken>> LockRanges()
        {
            PreLock();
            return _ranges = _spans.Count > 0
                                ? _spans
                                : IFilteredTokenEnumerableProvider.EmptyRange;
        }

        /// <summary>
        /// Locks these <see cref="Spans"/> as a each/range/token enumeration. See <see cref="GetEachGroups"/>.
        /// <para>
        /// This must not already be locked and, if the <see cref="Spans"/> is not empty, the last
        /// <see cref="TokenSpan.End"/> must not be greater than <see cref="SourceCode.Tokens"/>'s count
        /// otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </para>
        /// </summary>
        /// <returns>These Spans as a each/range/token enumeration.</returns>
        public IEnumerable<IEnumerable<IEnumerable<SourceToken>>> LockEachGroups()
        {
            PreLock();
            return _eachGroups = _spans.Count > 0
                                    ? _spans.Select( r => new IEnumerable<SourceToken>[] { r } )
                                    : IFilteredTokenEnumerableProvider.EmptyFilteredTokens;
        }

        void PreLock()
        {
            Throw.CheckState( !IsLocked );
            if( _spans.Count > 0 )
            {
                Throw.CheckState( _spans[^1].Span.End <= _editor.Code.Tokens.Count );
                _editor.Track( this );
            }
        }

        /// <summary>
        /// Gets these <see cref="Spans"/> as ranges of <see cref="SourceToken"/>.
        /// <para>
        /// <see cref="LockRanges"/> must have been called or an <see cref="InvalidOperationException"/> is thrown.
        /// </para>
        /// </summary>
        /// <returns>These Spans as ranges of <see cref="SourceToken"/>.</returns>
        public IEnumerable<IEnumerable<SourceToken>> GetRanges()
        {
            Throw.CheckState( IsLockedAsRanges );
            return _spans;
        }

        /// <summary>
        /// Gets these <see cref="Spans"/> as a each/range/token enumeration (see <see cref="IFilteredTokenEnumerableProvider"/>):
        /// each groups contains a unique list of <see cref="SourceToken"/>.
        /// <para>
        /// <see cref="LockEachGroups"/> must have been called or an <see cref="InvalidOperationException"/> is thrown.
        /// </para>
        /// </summary>
        /// <returns>These Spans as a each/range/token enumeration.</returns>
        public IEnumerable<IEnumerable<IEnumerable<SourceToken>>> GetEachGroups()
        {
            Throw.CheckState( IsLockedAsEachGroups );
            return _eachGroups;
        }

        /// <summary>
        /// Gets the spans.
        /// </summary>
        public IEnumerable<TokenSpan> Spans => _spans.Select( r => r.Span );

        /// <summary>
        /// Gets the count of <see cref="Spans"/>.
        /// </summary>
        public int Count => _spans.Count;

        /// <summary>
        /// Gets the span at <paramref name="idx"/>.
        /// </summary>
        /// <param name="idx">The span index.</param>
        /// <returns>The span.</returns>
        public TokenSpan SpanAt( int idx ) => _spans[idx].Span;

        /// <summary>
        /// Updates the <see cref="TokenSpan"/> at a specified index.
        /// The new span must not overlaps with the previous or the next token
        /// otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// <para>
        /// This can be only called when <see cref="IsLocked"/> is false.
        /// </para>
        /// </summary>
        /// <param name="idx">The index of the <see cref="TokenSpan"/>.</param>
        /// <param name="span">The new <see cref="TokenSpan.</param>
        /// <returns></returns>
        public void SetSpanAt( int idx, TokenSpan span )
        {
            Throw.CheckState( !IsLocked );
            // Let the accessor check the index.
            var range = _spans[idx];
            if( idx > 0 )
            {
                var prev = _spans[idx-1];
                if( prev.Span.End > span.Beg )
                {
                    Throw.ArgumentException( "span", "Overlaps the previous TokenSpan." );
                }
            }
            var nextIdx = idx + 1;
            if( nextIdx < _spans.Count )
            {
                var next = _spans[nextIdx];
                if( span.End > next.Span.Beg )
                {
                    Throw.ArgumentException( "span", "Overlaps the next TokenSpan." );
                }
            }
            range.Span = span;
        }

        /// <summary>
        /// Removes a previously appended span at a specified index.
        /// </summary>
        /// <param name="idx">The index of the span to remove.</param>
        public void RemoveAt( int idx )
        {
            Throw.CheckState( !IsLocked );
            _spans.RemoveAt( idx );
        }

        /// <summary>
        /// Appends a new span that must be after the last <see cref="Spans"/>.
        /// No check is done against the <see cref="SourceCode.Tokens"/> length here.
        /// <para>
        /// This can be only called when <see cref="IsLocked"/> is false.
        /// </para>
        /// </summary>
        /// <param name="span">New span to append.</param>
        public void AppendSpan( TokenSpan span )
        {
            Throw.CheckState( !IsLocked );
            Throw.CheckArgument( _spans.Count == 0 || span.Beg > _spans[^1].Span.End );
            Throw.CheckArgument( span.End <= _editor.Code.Tokens.Count );
            _spans.Add( new Range( _editor, span ) );
        }

        internal void OnRemoveTokens( TokenSpan removedHead, int eLimit )
        {
            for( int i = 0; i < _spans.Count; ++i )
            {
                var r = _spans[i];
                r.OnRemoveTokens( eLimit, removedHead.Length );
                var newSpan = r.Span.Remove( removedHead );
                if( newSpan.IsEmpty )
                {
                    _spans.RemoveAt( i-- );
                }
                else r.Span = newSpan;
            }
        }

        internal void OnInsertTokens( int index, int count, bool insertBefore, int eLimit )
        {
            foreach( var r in _spans )
            {
                r.OnInsertTokens( eLimit, count );
                if( index < r.Span.Beg || (insertBefore && index == r.Span.Beg) )
                {
                    r.Span = new TokenSpan( r.Span.Beg + count, r.Span.End + count );
                }
                else if( index < r.Span.End )
                {
                    r.Span = new TokenSpan( r.Span.Beg, r.Span.End + count );
                }
            }
        }

    }


}
