using CK.Core;
using System;
using System.Collections.Immutable;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// Supports 2 modes:
/// <list type="number">
///     <item>As an independent operator, it splits each match into matched patterns.</item>
///     <item>As the {span specification} where "Patterh" operator, it filters out the matches that don't contain at least one pattern.</item>
/// </list>
/// Implements Knuth-Morris-Pratt find algorithm.
/// </summary>
public sealed partial class TokenPatternOperator : ITokenFilterOperator
{
    readonly RawString _tokenPattern;
    readonly ImmutableArray<Token> _tokens;
    readonly int[] _prefixTable;
    readonly bool _whereMode;

    /// <summary>
    /// Initializes a new token span filter.
    /// </summary>
    /// <param name="tokens">The tokens. Must not be empty or default.</param>
    public TokenPatternOperator( RawString tokenPattern, ImmutableArray<Token> tokens, bool whereMode )
    {
        Throw.CheckArgument( !tokens.IsDefaultOrEmpty );
        _tokenPattern = tokenPattern;
        _tokens = tokens;
        _whereMode = whereMode;
        _prefixTable = BuildPrefixTable( tokens );
    }

    static int[] BuildPrefixTable( ImmutableArray<Token> tokens )
    {
        var prefixTable = new int[tokens.Length + 1];
        prefixTable[0] = -1;
        int i = 0;
        int prefixLength = -1;
        while( i < tokens.Length )
        {
            while( prefixLength >= 0
                   && !tokens[i].Text.Span.Equals( tokens[prefixLength].Text.Span, StringComparison.Ordinal ) )
            {
                prefixLength = prefixTable[prefixLength];
            }
            prefixTable[++i] = ++prefixLength;
        }

        return prefixTable;
    }

    /// <summary>
    /// Collects this operator.
    /// </summary>
    /// <param name="collector">The operator collector.</param>
    public void Activate( Action<ITokenFilterOperator> collector ) => collector( this );

    void ITokenFilterOperator.Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        var m = new TokenMatcher( _tokens, _prefixTable );
        if( _whereMode )
        {
            m.FilterWhere( context, input );
        }
        else
        {
            m.CreateMatches( context, input );
        }
    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable )
        {
            b.Append( _whereMode ? "[Where] \"" : "[Pattern] \"" );
            return _tokens.WriteCompact( b ).Append( '"' );
        }
        if( _whereMode ) b.Append( "where " );
        return _tokenPattern.Lines.Length > 1
                ? b.Append( _tokenPattern.OpeningQuotes ).AppendLine()
                   .Append( _tokenPattern.TextLines ).AppendLine()
                   .Append( _tokenPattern.ClosingQuotes )
                : b.Append( _tokenPattern.Text );
    }

    public override string ToString() => Describe( new StringBuilder(), parsable: true ).ToString();

}
