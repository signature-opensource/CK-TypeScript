using CK.Core;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
                b.AppendLine();
                using( _editor.Monitor.OpenError( b.ToString() ) )
                {
                    WriteFullPath( b, withMatches: true );
                    _editor.Monitor.Error( b.ToString() );
                }
            }
        }

        void ITokenFilterOperatorContext.SetResult( TokenMatch[] result )
        {
            Throw.CheckArgument( result != null );
            if( !_hasFailed )
            {
                if( !result.CheckValid( _editor._tokens, out var error ) )
                {
                    Throw.ArgumentException( nameof( result ), error );
                }
                _matches = result;
            }
        }

        void ITokenFilterOperatorContext.SetResult( TokenFilterBuilder builder )
        {
            Throw.CheckArgument( builder != null );
            if( !_hasFailed )
            {
                var builderResult = builder.ExtractResult();
                Throw.CheckState( builderResult.Length == 0 || builderResult[^1].Span.End <= _editor._tokens.Count );
                _matches = builderResult;
            }
        }

        void ITokenFilterOperatorContext.SetUnchangedResult()
        {
            Throw.CheckState( _previous != null );
            if( !_hasFailed )
            {
                _matches = _previous._matches;
            }
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

        StringBuilder WriteFullPath( StringBuilder b, bool withMatches = false )
        {
            if( _previous == null )
            {
                b.Append( "(all)" );
            }
            else
            {
                Throw.DebugAssert( _operator != null );
                _previous.WriteFullPath( b, withMatches ).Append( _previous._syntaxBorder ? " |> " : " > " );
                _operator.Describe( b, parsable: false );
            }
            if( withMatches )
            {
                b.AppendLine().Append( "   " );
                if( _matches == null )
                {
                    b.Append( "<null>" );
                }
                else if( _matches.Length == 0 )
                {
                    b.Append( "<no matches>" );
                }
                else if( !_matches.CheckValid( _editor._code.Tokens, out var error ) )
                {
                    b.Append( "Invalid matches: " ).Append( error );
                }
                else
                {
                    var e = new TokenFilterEnumerator( _matches, _editor._code.Tokens );
                    if( e.IsSingleEach )
                    {
                        e.NextEach( skipEmpty: false );
                        if( !e.NextMatch() )
                        {
                            b.Append( "Single each with empty matches." );
                        }
                        else
                        {
                            bool atLeastOne = false;
                            do
                            {
                                if( atLeastOne ) b.Append( ", " );
                                atLeastOne = true;
                                b.Append( e.CurrentMatch.Span );
                            }
                            while( e.NextMatch() );
                        }
                    }
                    else
                    {
                        while( e.NextEach( skipEmpty: false ) )
                        {
                            if( e.CurrentEachIndex > 0 )
                            {
                                b.AppendLine().Append( "   " );
                            }
                            b.Append( "Each n°" ).Append( e.CurrentEachIndex ).Append( ':' ).AppendLine();
                            b.Append( "      " );
                            bool atLeastOne = false;
                            do
                            {
                                if( atLeastOne ) b.Append( ", " );
                                atLeastOne = true;
                                b.Append( e.CurrentMatch.Span );
                            }
                            while( e.NextMatch() );
                        }
                    }
                }
                b.AppendLine();
            }
            return b;
        }

        public override string ToString() => WriteFullPath( new StringBuilder() ).ToString();

    }

}
