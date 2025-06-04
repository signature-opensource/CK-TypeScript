using System;
using System.Collections.Immutable;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// A <see cref="ITokenFilterOperator"/> that filers matches that
/// contains a <see cref="InjectionPoint"/>.
/// </summary>
public sealed class InjectionPointFilterOperator : ITokenFilterOperator
{
    readonly bool _include;
    readonly InjectionPoint _target;

    public InjectionPointFilterOperator( bool include, InjectionPoint target )
    {
        _include = include;
        _target = target;
    }

    /// <summary>
    /// Collects this operator.
    /// </summary>
    /// <param name="collector">The operator collector.</param>
    public void Activate( Action<ITokenFilterOperator> collector ) => collector( this );

    public void Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        var builder = context.SharedBuilder;
        var e = input.CreateTokenEnumerator();
        while( e.NextEach( skipEmpty: false ) )
        {
            while( e.NextMatch() )
            {
                bool found = false;
                while( e.NextToken() )
                {
                    found = Find( e.Token.Token.LeadingTrivias, _target.Name )
                            || Find( e.Token.Token.TrailingTrivias, _target.Name );
                    if( found )
                    {
                        break;
                    }
                }
                if( _include == found )
                {
                    builder.AddMatch( e.CurrentMatch.Span );
                }
                builder.StartNewEach( skipEmpty: false );
            }
        }
        context.SetResult( builder );

        static bool Find( ImmutableArray<Trivia> trivias, ReadOnlySpan<char> name )
        {
            foreach( var t in trivias )
            {
                if( t.CommentStartLength != 0
                    && InjectionPointFinder.ParseTrivia( t, name, out _, out _, out _, out _ ) )
                {
                    return true;
                }
            }
            return false;
        }

    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable )
        {
            b.Append( _include ? "[Include] " : "[Exclude] " ).Append( _target.Text );
        }
        else
        {
            b.Append( _include ? "where " : "unless " ).Append( _target.Text );
        }
        return b;
    }

    public override string ToString() => Describe( new StringBuilder(), parsable: true ).ToString();

}

