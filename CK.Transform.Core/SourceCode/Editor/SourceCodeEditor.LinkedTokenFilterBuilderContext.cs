using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{
    internal sealed class LinkedTokenFilterBuilderContext : ITokenFilterBuilderContext
    {
        readonly SourceCodeEditor _editor;
        readonly IFilteredTokenEnumerableProvider? _provider;
        readonly LinkedTokenFilterBuilderContext? _previous;
        readonly IEnumerable<IEnumerable<IEnumerable<SourceToken>>> _tokens;
        readonly int _index;
        bool _syntaxBorder;
        bool _isAllTransparent;

        internal LinkedTokenFilterBuilderContext( SourceCodeEditor editor,
                                                  IFilteredTokenEnumerableProvider provider,
                                                  LinkedTokenFilterBuilderContext previous )
        {
            _editor = editor;
            _provider = provider;
            _previous = previous;
            _index = previous._index + 1;
            var p = provider.GetFilteredTokenProjection();
            if( p == IFilteredTokenEnumerableProvider.EmptyProjection )
            {
                _isAllTransparent = true;
                _tokens = previous._tokens;
            }
            else
            {
                _tokens = p.Invoke( this, previous._tokens );
            }
        }

        // Root ctor.
        internal LinkedTokenFilterBuilderContext( SourceCodeEditor editor )
        {
            _editor = editor;
            _tokens = [[editor._sourceTokens]];
            _syntaxBorder = true;
        }

        internal LinkedTokenFilterBuilderContext? Previous => _previous;

        internal IFilteredTokenEnumerableProvider? Provider => _provider;

        internal int Index => _index;

        [MemberNotNullWhen(false, nameof( Previous ), nameof( Provider ) )]
        internal bool IsRoot => _index == 0;

        internal SourceCodeEditor Editor => _editor;

        internal IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Tokens => _tokens;

        internal void SetSyntaxBorder() => _syntaxBorder = true;

        IReadOnlyList<Token> ITokenFilterBuilderContext.Tokens => _editor.Code.Tokens;

        public bool HasError => _editor.HasError;

        public bool IsAllTransparent => _isAllTransparent;

        public void Error( string errorMessage )
        {
            var monitor = _editor.Monitor;
            using( monitor.OpenError( errorMessage ) )
            {
                var b = new StringBuilder();
                monitor.Trace( $"Current filter: {WriteFullPath( b ).ToString()}" );
                DumpFilterResults( monitor, this, b );
            }

            static void DumpFilterResults( IActivityMonitor monitor,
                                           LinkedTokenFilterBuilderContext c,
                                           StringBuilder b )
            {
                if( c.IsRoot ) return;
                DumpFilterResults( monitor, c.Previous, b );
                if( c.IsAllTransparent )
                {
                    monitor.Trace( $"Filter n°{c.Index}: all (transparent)." );
                }
                else
                {
                    b.Clear();
                    int eachCount = c.Tokens.Count();
                    var eachSummary = eachCount == 0
                                        ? "Empty"
                                        : eachCount == 1
                                            ? "No each group"
                                            : $"{eachCount} each groups";
                    string summary = $"Filter n°{c.Index}: {c.Provider.Describe( b, false )} - {eachSummary}";
                    if( eachCount == 0 )
                    {
                        monitor.Trace( summary );
                    }
                    else
                    {
                        using( monitor.OpenTrace( $"Filter n°{c.Index}: {c.Provider.Describe( b, false ).ToString()} - {eachSummary}." ) )
                        {
                            if( eachCount > 1 )
                            {
                                int eachNumber = 0;
                                foreach( var each in c.Tokens )
                                {
                                    DumpRanges( monitor, $"Each group n°{eachNumber}: ", each, b );
                                    ++eachNumber;
                                }
                            }
                            else
                            {
                                DumpRanges( monitor, $"Ranges: ", c.Tokens.First(), b );
                            }
                        }
                    }
                }

                static void DumpRanges( IActivityMonitor monitor,
                                        string prefix,
                                        IEnumerable<IEnumerable<SourceToken>> each,
                                        StringBuilder b )
                {
                    var rangeCount = each.Count();
                    if( rangeCount == 0 )
                    {
                        monitor.Trace( $"{prefix}Empty." );
                    }
                    else
                    {
                        using( monitor.OpenTrace( $"{prefix}{rangeCount} ranges." ) )
                        {
                            b.Clear();
                            int iRange = 0;
                            foreach( var r in each )
                            {
                                b.Append( "--- (range n°" ).Append( ++iRange ).AppendLine( ") ---" );
                                r.Select( t => t.Token ).Write( b ).AppendLine();
                                b.AppendLine( "---" );
                            }
                            monitor.Trace( b.ToString() );
                        }
                    }
                }

            }
        }

        public IEnumerable<SourceToken> GetSourceTokens( SourceSpan span )
        {
            Throw.CheckState( !span.IsDetached );
            return new SourceSpanTokenEnumerable( _editor, span );
        }

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
                Throw.DebugAssert( _provider != null );
                _previous.WriteFullPath( b ).Append( _previous._syntaxBorder ? " |> " : " > " );
                _provider.Describe( b, parsable: false );
            }
            return b;
        }


        public override string ToString() => WriteFullPath( new StringBuilder() ).ToString();
    }
}
