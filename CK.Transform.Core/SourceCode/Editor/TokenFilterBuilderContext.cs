using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

public readonly struct TokenFilterBuilderContext
{
    readonly SourceCodeEditor _editor;

    internal TokenFilterBuilderContext( SourceCodeEditor editor )
    {
        _editor = editor;
    }

    public bool HasError => _editor.HasError;

    public void Error( string errorMessage ) => _editor.Monitor.Error( errorMessage );

    public IReadOnlyList<Token> Tokens => _editor.Code.Tokens;

    public IEnumerable<SourceToken> GetSourceTokens( SourceSpan span )
    {
        Throw.CheckState( !span.IsDetached );
        return new SourceCodeEditor.SourceSpanTokenEnumerable( _editor, span );
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
}
