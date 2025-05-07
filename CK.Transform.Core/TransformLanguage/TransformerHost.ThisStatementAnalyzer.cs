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

        internal protected override object ParseSpanSpec( Language language, RawString tokenSpec )
        {
            var singleSpanType = tokenSpec.InnerText.Span.Trim();
            if( singleSpanType.Length > 0 )
            {
                return singleSpanType switch
                {
                    "statement" => new SingleSpanTypeFilter( typeof( TransformStatement ), "{statement}" ),
                    "in" => new SingleSpanTypeFilter( typeof( InScope ), "{in}" ),
                    "replace" => new SingleSpanTypeFilter( typeof( ReplaceStatement ), "{replace}" ),
                    _ => $"""
                         Invalid span type '{singleSpanType}'. Allowed are "statement", "in", "replace".
                         """
                };
            }
            return IFilteredTokenEnumerableProvider.Empty;
        }


        protected override void ParseStandardMatchPattern( Language language, ref TokenizerHead head )
        {
            while( head.EndOfInput == null )
            {
                if( head.LowLevelTokenType == TokenType.LessThan )
                {
                    if( InjectionPoint.Match( ref head ) == null )
                    {
                        Throw.DebugAssert( head.FirstError != null );
                        break;
                    }
                }
                else if( head.LowLevelTokenType is TokenType.DoubleQuote )
                {
                    if( RawString.Match( ref head ) == null )
                    {
                        Throw.DebugAssert( head.FirstError != null );
                        break;
                    }
                }
                else if( head.LowLevelTokenType is TokenType.OpenBrace )
                {
                    if( RawString.MatchAnyQuote( ref head, '{', '}' ) == null )
                    {
                        Throw.DebugAssert( head.FirstError != null );
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
