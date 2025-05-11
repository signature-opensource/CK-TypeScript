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

    internal sealed class LinkedTokenFilterBuilderContext : ITokenFilterBuilderContext
    {
        readonly SourceCodeEditor _editor;
        readonly IFilteredTokenEnumerableProvider? _provider;
        readonly LinkedTokenFilterBuilderContext? _previous;
        readonly IEnumerable<IEnumerable<IEnumerable<SourceToken>>> _tokens;
        readonly int _index;
        bool _syntaxBorder;
        bool _isTransparent;

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
                _isTransparent = true;
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


        internal int Index => _index;

        internal SourceCodeEditor Editor => _editor;

        public LinkedTokenFilterBuilderContext? Previous => _previous;

        internal void SetSyntaxBorder() => _syntaxBorder = true;

        internal bool IsTransparent => _isTransparent;

        [MemberNotNullWhen(false, nameof( Previous ), nameof( Provider ) )]
        public bool IsRoot => _index == 0;

        public bool IsSyntaxBorder => _syntaxBorder;

        public IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Tokens => _tokens;

        public IFilteredTokenEnumerableProvider? Provider => _provider;

        IReadOnlyList<Token> ITokenFilterBuilderContext.UnfilteredTokens => _editor.Code.Tokens;

        ITokenFilterBuilderContext? ITokenFilterBuilderContext.Previous => _previous;

        public bool HasEditorError => _editor.HasError;

        public void Fail( string failureMessage )
        {
            failureMessage ??= "<no failure message>";
            var b = new StringBuilder( failureMessage );
            b.AppendLine().Append( "Current filter: " );
            WriteFullPath( b );
            _editor.SetTokenFilteringError( new TokenFilteringError( this, b.ToString() ) );
        }

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
                Throw.DebugAssert( _provider != null );
                _previous.WriteFullPath( b ).Append( _previous._syntaxBorder ? " |> " : " > " );
                _provider.Describe( b, parsable: false );
            }
            return b;
        }

        public override string ToString() => WriteFullPath( new StringBuilder() ).ToString();
    }
}
