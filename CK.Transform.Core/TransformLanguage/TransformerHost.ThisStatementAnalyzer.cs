using CK.Core;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.Core;


public sealed partial class TransformerHost
{
    sealed class ThisStatementAnalyzer : TransformStatementAnalyzer
    {
        public ThisStatementAnalyzer( TransformLanguage language )
            : base( language )
        {
        }

        // /// <summary>
        // /// There is currently no specific statements to transform transformers: the base
        // /// ParseStatement is fine.
        // /// </summary>
        // protected override TransformStatement? ParseStatement( ref TokenizerHead head )
        // {
        //     return base.ParseStatement( ref head );
        // }

        protected override object ParseSpanSpec( Language language, RawString tokenSpec )
        {
            var singleSpanType = tokenSpec.InnerText.Span.Trim();
            if( singleSpanType.Length > 0 )
            {
                var sType = singleSpanType switch
                {
                    "statement" => typeof( TransformStatement ),
                    "in" => typeof( InScope ),
                    "replace" => typeof( ReplaceStatement ),
                    _ => null
                };
                if( sType == null )
                {
                    return $"""
                            Invalid span type '{singleSpanType}'. Allowed are "statement", "in", "replace".
                            """;
                }
                return new SingleSpanTypeFilter( sType );
            }
            return IFilteredTokenEnumerableProvider.Empty;
        }


        protected override void ParseStandardMatchPattern( ref TokenizerHead head )
        {
            while( head.EndOfInput == null )
            {
                if( head.LowLevelTokenType == TokenType.LessThan )
                {
                    if( InjectionPoint.Match( ref head ) == null )
                    {
                        Throw.DebugAssert( head.FirstParseError != null );
                        break;
                    }
                }
                else if( head.LowLevelTokenType is TokenType.DoubleQuote or TokenType.OpenBrace )
                {
                    if( RawString.Match( ref head ) == null )
                    {
                        Throw.DebugAssert( head.FirstParseError != null );
                        break;
                    }
                }
                else if( head.LowLevelTokenType == TokenType.None )
                {
                    head.AppendError( $"Unknown token '{head.Head[0]}'.", 1 );
                    break;
                }
                else
                {
                    head.AcceptLowLevelToken();
                }
            }
        }

    }

}
