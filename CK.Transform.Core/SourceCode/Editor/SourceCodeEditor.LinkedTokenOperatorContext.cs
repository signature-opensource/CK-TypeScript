using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{

    internal sealed class LinkedTokenOperatorContext : ITokenFilterOperatorSource, ITokenFilterOperatorContext
    {
        readonly SourceCodeEditor _editor;
        readonly ITokenFilterOperator? _operator;
        readonly LinkedTokenOperatorContext? _previous;
        readonly int _index;
        TokenMatch[]? _matches;
        bool _syntaxBorder;
        bool _hasFailed;

        internal LinkedTokenOperatorContext( SourceCodeEditor editor,
                                             ITokenFilterOperator op,
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

        public TokenFilter Tokens
        {
            get
            {
                Throw.DebugAssert( "Otherwise this property is not accessible.", _matches != null );
                return new TokenFilter( _matches );
            }
        }

        // Implements the ITokenFilterOperatorSourceContext.CreateTokenEnumerator(): the matches
        // have necessarily been succefully computed.
        public TokenFilterEnumerator CreateTokenEnumerator()
        {
            Throw.DebugAssert( "Otherwise this property is not accessible.", _matches != null );
            return new TokenFilterEnumerator( _matches, _editor._tokens );
        }


        internal TokenMatch[]? Setup()
        {
            _hasFailed = false;
            _matches = null;
            if( IsRoot )
            {
                int count = _editor.Code.Tokens.Count;
                _matches = count == 0
                            ? []
                            : [new TokenMatch( 0, 0, new TokenSpan( 0, count ) )];
            }
            else
            {
                var prevMatches = _previous.Setup();
                if( prevMatches != null )
                {
                    _operator.Apply( this, _previous );
                    if( _matches == null && !_hasFailed )
                    { 
                        Throw.InvalidOperationException( $"'{_operator.GetType().ToCSharpName()}.Apply method must call {nameof( ITokenFilterOperatorContext.SetResult )}, {nameof( ITokenFilterOperatorContext.SetFailedResult )} or {nameof( ITokenFilterOperatorContext.SetUnchangedResult )}." );
                    }
                }
            }
            return _matches;
        }

        void ITokenFilterOperatorContext.SetFailedResult( string failureMessage, ITokenFilterEnumerator? current )
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

        void ITokenFilterOperatorContext.SetResult( TokenMatch[] result )
        {
            Throw.CheckArgument( result != null );
            if( !result.CheckValid( _editor._tokens, out var error ) )
            {
                Throw.ArgumentException( nameof( result ), error );
            }
            _matches = result;
        }

        void ITokenFilterOperatorContext.SetResult( TokenFilterBuilder builder )
        {
            Throw.CheckArgument( builder != null );
            var builderResult = builder.ExtractResult();
            Throw.CheckState( builderResult.Length == 0 || builderResult[^1].Span.End <= _editor._tokens.Count );
            _matches = builderResult;
        }

        void ITokenFilterOperatorContext.SetUnchangedResult()
        {
            Throw.CheckState( _previous != null );
            _matches = _previous._matches;
        }


        public ITokenFilterOperator? Operator => _operator;

        public TokenFilterBuilder SharedBuilder => _editor._sharedBuilder;

        IReadOnlyList<Token> ITokenFilterOperatorContext.UnfilteredTokens => _editor.Code.Tokens;

        ITokenFilterOperatorSource? ITokenFilterOperatorSource.Previous => _previous;

        public SourceSpan? GetDeepestSpanAt( int index )
        {
            return _editor.Code.Spans.GetChildrenSpanAt( index );
        }

        public SourceSpan? GetDeepestSpanAt( int index, Type spanType )
        {
            var s = _editor.Code.Spans.GetChildrenSpanAt( index );
            while( s != null )
            {
                if( spanType.IsAssignableFrom( s.GetType() ) ) return s;
                s = s.Parent;
            }
            return null;
        }

        public SourceSpan? GetTopSpanAt( int index, Type spanType, TokenSpan scope )
        {
            // This is not optimized at all. This can be done later.
            var s = GetDeepestSpanAt( index, spanType );
            if( s == null || !(scope.IsEmpty || scope.ContainsOrEquals( s.Span )) )
            {
                return null;
            }
            var sAbove = s.Parent;
            while( sAbove != null && (scope.IsEmpty || scope.ContainsOrEquals( sAbove.Span )) )
            {
                s = sAbove;
                sAbove = s.Parent;
            }
            return s;
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
