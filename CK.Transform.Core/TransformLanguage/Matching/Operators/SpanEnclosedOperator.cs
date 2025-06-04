using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// A <see cref="ITokenFilterOperator"/> that splits matches to the deepest
/// spans defined by opening/closing pair of one (or more) tokens.
/// <para>
/// This is like the <see cref="SpanTypeOperator"/> but "dynamic" as no
/// <see cref="SourceSpan"/> are required to exist.
/// </para>
/// </summary>
public sealed class SpanEnclosedOperator : ITokenFilterOperator
{
    readonly Func<IReadOnlyList<Token>, int, EnclosingTokenType> _enclosing;
    readonly string _displayName;

    public void Activate( Action<ITokenFilterOperator> collector ) => collector( this );

    public SpanEnclosedOperator( string displayName, Func<IReadOnlyList<Token>, int, EnclosingTokenType> enclosing )
    {
        _enclosing = enclosing;
        _displayName = displayName;
    }

    public void Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        var builder = context.SharedBuilder;
        var spanCollector = new TokenSpanDeepestCollector();
        var e = input.CreateTokenEnumerator();
        while( e.NextEach( skipEmpty: false ) )
        {
            while( e.NextMatch() )
            {
                var spans = new EnclosedSpanDeepestEnumerator( context.UnfilteredTokens, e.CurrentMatch.Span, _enclosing );
                while( spans.MoveNext() )
                {
                    builder.AddMatch( spans.Current );
                }
            }
            builder.StartNewEach( skipEmpty: false );
        }
        context.SetResult( builder );
    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable ) b.Append( "[SpanEnclosed] " );
        return b.Append( _displayName );
    }

    public override string ToString() => Describe( new StringBuilder(), true ).ToString();

}
