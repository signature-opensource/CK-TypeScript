using CK.Core;
using System;
using System.Collections.Generic;
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

        internal LinkedTokenFilterBuilderContext( SourceCodeEditor editor,
                                                  IFilteredTokenEnumerableProvider provider,
                                                  LinkedTokenFilterBuilderContext previous )
        {
            _editor = editor;
            _provider = provider;
            _previous = previous;
            _index = previous._index + 1;
            _tokens = provider.GetFilteredTokenProjection().Invoke( this, previous._tokens );
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

        internal SourceCodeEditor Editor => _editor;

        internal IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Tokens => _tokens;

        internal void SetSyntaxBorder() => _syntaxBorder = true;

        IReadOnlyList<Token> ITokenFilterBuilderContext.Tokens => _editor.Code.Tokens;

        public bool HasError => _editor.HasError;

        public void Error( string errorMessage ) => _editor.Monitor.Error( errorMessage );

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

        void FillSyntaxPath( Span<LinkedTokenFilterBuilderContext> path )
        {
            Throw.DebugAssert( path.Length == _index );
            var p = this;
            int syntaxBorder = path.Length - 1;
            for( int i = syntaxBorder; i >= 0; --i )
            {
                path[i] = p;
                Throw.DebugAssert( p._previous != null );
                p = p._previous;
                if( p._syntaxBorder )
                {
                    int count = syntaxBorder - i;
                    if( count > 1 ) path.Slice( i, count ).Reverse();
                    syntaxBorder = i;
                }
            }
        }

        StringBuilder WriteFullPath( StringBuilder b )
        {
            var path = new LinkedTokenFilterBuilderContext[_index];
            FillSyntaxPath( path.AsSpan() );
            b.Append( "(all)" );
            foreach( var p in path )
            {
                Throw.DebugAssert( p._previous != null && p._provider != null );
                if( p._previous._syntaxBorder ) b.Append( " >" );
                b.Append( ' ' );
                p._provider.Describe( b, parsable: false );
            }
            return b;
        }

        public override string ToString() => WriteFullPath( new StringBuilder() ).ToString();
    }
}
