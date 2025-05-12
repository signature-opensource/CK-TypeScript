using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{

    internal sealed class LinkedTokenOperatorContext : IFilteredTokenOperatorSourceContext, IFilteredTokenOperatorContext
    {
        readonly SourceCodeEditor _editor;
        readonly IFilteredTokenOperator? _operator;
        readonly LinkedTokenOperatorContext? _previous;
        readonly int _index;
        FilteredTokenSpan[]? _filteredTokens;
        bool _syntaxBorder;
        bool _hasFailed;

        internal LinkedTokenOperatorContext( SourceCodeEditor editor,
                                             IFilteredTokenOperator op,
                                             LinkedTokenOperatorContext previous )
        {
            _editor = editor;
            _operator = op;
            _previous = previous;
            _index = previous._index + 1;
        }

        // Root ctor.
        internal LinkedTokenOperatorContext( SourceCodeEditor editor )
        {
            _editor = editor;
            _syntaxBorder = true;
        }


        internal int Index => _index;

        internal SourceCodeEditor Editor => _editor;

        public LinkedTokenOperatorContext? Previous => _previous;

        internal void SetSyntaxBorder() => _syntaxBorder = true;

        [MemberNotNullWhen( false, nameof( Previous ), nameof( Operator ), nameof( _previous ), nameof( _operator ) )]
        public bool IsRoot => _index == 0;

        public bool IsSyntaxBorder => _syntaxBorder;

        public bool HasFailed => _hasFailed;

        public IReadOnlyList<FilteredTokenSpan> FilteredTokens => EnsureFilteredTokens();
        
        FilteredTokenSpan[] EnsureFilteredTokens()
        {
            if( _filteredTokens == null )
            {
                if( IsRoot )
                {
                    int count = _editor.Code.Tokens.Count;
                    _filteredTokens = count == 0
                                ? []
                                : [new FilteredTokenSpan( 0, 0, new TokenSpan( 0, count ) )];
                }
                else
                {
                    var prevMatches = _previous.EnsureFilteredTokens();
                    if( _previous.HasFailed )
                    {
                        _hasFailed = true;
                        _filteredTokens = prevMatches;
                    }
                    else
                    {
                        _operator.Apply( this, prevMatches );
                        if( _filteredTokens == null )
                        {
                            Throw.InvalidOperationException( $"'{_operator.GetType().ToCSharpName()}.Apply method must call {nameof(IFilteredTokenOperatorContext.SetResult)}, {nameof( IFilteredTokenOperatorContext.SetFailedResult )} or {nameof( IFilteredTokenOperatorContext.SetUnchangedResult )}." );
                        }
                    }
                }
            }
            return _filteredTokens;
        }

        void IFilteredTokenOperatorContext.SetFailedResult( string failureMessage, IFilteredTokenSpanEnumerator? current )
        {
            _hasFailed = true;
            failureMessage ??= "<no failure message>";
            var b = new StringBuilder( failureMessage );
            b.AppendLine().Append( "Current filter: " );
            WriteFullPath( b );
            if( current == null )
            {
                _editor.Monitor.Error( b.ToString() );
            }
            else
            {
                using( _editor.Monitor.OpenError( b.ToString() ) )
                {
                    _editor.Monitor.Error( "TODO" );
                }
            }
        }

        void IFilteredTokenOperatorContext.SetResult( FilteredTokenSpan[] result )
        {
            Throw.CheckArgument( result != null );
            result.CheckInvariants( _editor._tokens );
            _filteredTokens = result;
        }

        void IFilteredTokenOperatorContext.SetResult( FilteredTokenSpanListBuilder builder )
        {
            Throw.CheckArgument( builder != null );
            var builderResult = builder.ExtractResult();
            Throw.CheckState( builderResult.Length == 0 || builderResult[^1].Span.End <= _editor._tokens.Count );
            _filteredTokens = builderResult;
        }

        void IFilteredTokenOperatorContext.SetUnchangedResult()
        {
            Throw.CheckState( _previous != null );
            _filteredTokens = _previous._filteredTokens;
        }


        public IFilteredTokenOperator? Operator => _operator;

        public FilteredTokenSpanListBuilder SharedBuilder => _editor._sharedBuilder;

        IReadOnlyList<Token> IFilteredTokenOperatorContext.UnfilteredTokens => _editor.Code.Tokens;

        IFilteredTokenOperatorSourceContext IFilteredTokenOperatorContext.Previous => _previous!;

        IFilteredTokenOperatorSourceContext? IFilteredTokenOperatorSourceContext.Previous => _previous;

        public bool HasEditorError => _editor.HasError;

        public IEnumerable<SourceToken> GetSourceTokens( SourceSpan span )
        {
            Throw.CheckState( !span.IsDetached );
            return new SourceSpanTokenEnumerable( _editor, span );
        }

        public DynamicSpans CreateDynamicSpan() => new DynamicSpans( _editor );

        public SourceSpan? GetDeepestSpanAt( int index )
        {
            return _editor.Code.Spans.GetSpanAt( index );
        }

        public SourceSpan? GetDeepestSpanAt( int index, Type spanType )
        {
            var s = _editor.Code.Spans.GetSpanAt( index );
            while( s != null )
            {
                if( spanType.IsAssignableFrom( s.GetType() ) ) return s;
                s = s.Parent;
            }
            return null;
        }

        StringBuilder WriteFullPath( StringBuilder b )
        {
            if( _previous == null )
            {
                b.Append( "(all)" );
            }
            else
            {
                Throw.DebugAssert( _operator != null );
                _previous.WriteFullPath( b ).Append( _previous._syntaxBorder ? " |> " : " > " );
                _operator.Describe( b, parsable: false );
            }
            return b;
        }

        public override string ToString() => WriteFullPath( new StringBuilder() ).ToString();

    }
}
