using System;

namespace CK.Transform.Core;

/// <summary>
/// Low-level token is a candidate token.
/// </summary>
/// <param name="NodeType">The detected candidate node type. Defaults to <see cref="NodeType.None"/>.</param>
/// <param name="Length">The candidate token length. Defaults to 0.</param>
public readonly record struct LowLevelToken( NodeType NodeType, int Length )
{

    /// <summary>
    /// Handles basic tokens.
    /// </summary>
    /// <param name="head">The head.</param>
    /// <returns>The low level token.</returns>
    public static LowLevelToken GetBasicTokenType( ReadOnlySpan<char> head )
    {
        if( head.Length == 0 ) return default;
        var c = head[0];
        var knownSingle = NodeTypeExtensions.GetSingleCharType( c );
        if( knownSingle == NodeType.None ) return default;
        if( head.Length > 1 )
        {
            c = head[1];
            switch( knownSingle )
            {
                case NodeType.Ampersand:
                    if( c == '&' )
                    {
                        if( head.Length > 2 && head[2] == '=' )
                        {
                            return new LowLevelToken( NodeType.AmpersandAmpersandEquals, 3 );
                        }
                        return new LowLevelToken( NodeType.AmpersandAmpersand, 2 );
                    }
                    if( c == '=' ) return new LowLevelToken( NodeType.AmpersandEquals, 2 );
                    break;
                case NodeType.Asterisk:
                    if( c == '=' ) return new LowLevelToken( NodeType.AsteriskEquals, 2 );
                    break;
                case NodeType.Bar:
                    if( c == '|' )
                    {
                        if( head.Length > 2 && head[2] == '=' )
                        {
                            return new LowLevelToken( NodeType.BarBarEquals, 3 );
                        }
                        return new LowLevelToken( NodeType.BarBar, 2 );
                    }

                    if( c == '=' ) return new LowLevelToken( NodeType.BarEquals, 2 );
                    break;
                case NodeType.Caret:
                    if( c == '=' ) return new LowLevelToken( NodeType.CaretEquals, 2 );
                    break;
                case NodeType.Dot:
                    if( c == '.' )
                    {
                        if( head.Length > 2 && head[2] == '.' )
                        {
                            return new LowLevelToken( NodeType.DotDotDot, 3 );
                        }
                        return new LowLevelToken( NodeType.DotDot, 2 );
                    }
                    break;
                case NodeType.Equals:
                    if( c == '=' )
                    {
                        if( head.Length > 2 && head[2] == '=' )
                        {
                            return new LowLevelToken( NodeType.EqualsEqualsEquals, 3 );
                        }
                        return new LowLevelToken( NodeType.EqualsEquals, 2 );
                    }
                    if( c == '>' )
                    {
                        return new LowLevelToken( NodeType.EqualsGreaterThan, 2 );
                    }
                    break;
                case NodeType.Exclamation:
                    if( c == '=' )
                    {
                        if( head.Length > 2 && head[2] == '=' )
                        {
                            return new LowLevelToken( NodeType.ExclamationEqualsEquals, 3 );
                        }
                        return new LowLevelToken( NodeType.ExclamationEquals, 2 );
                    }
                    break;
                case NodeType.Percent:
                    if( c == '=' ) return new LowLevelToken( NodeType.PercentEquals, 2 );
                    break;
                case NodeType.Minus:
                    if( c == '=' ) return new LowLevelToken( NodeType.MinusEquals, 2 );
                    if( c == '-' ) return new LowLevelToken( NodeType.MinusMinus, 2 );
                    break;
                case NodeType.Plus:
                    if( c == '=' ) return new LowLevelToken( NodeType.PlusEquals, 2 );
                    if( c == '+' ) return new LowLevelToken( NodeType.PlusPlus, 2 );
                    break;
                case NodeType.Slash:
                    if( c == '=' ) return new LowLevelToken( NodeType.SlashEquals, 2 );
                    break;
                case NodeType.LessThan:
                    if( c == '<' )
                    {
                        if( head.Length > 2 )
                        {
                            if( head[2] == '<' ) return new LowLevelToken( NodeType.LessThanLessThanLessThan, 3 );
                            if( head[2] == '=' ) return new LowLevelToken( NodeType.LessThanLessThanEquals, 3 );
                        }
                        return new LowLevelToken( NodeType.LessThanLessThan, 2 );
                    }
                    if( c == '=' )
                    {
                        return new LowLevelToken( NodeType.LessThanEquals, 2 );
                    }
                    break;
                case NodeType.GreaterThan:
                    if( c == '>' )
                    {
                        if( head.Length > 2 )
                        {
                            if( head[2] == '>' )
                            {
                                if( head.Length > 3 )
                                {
                                    if( head[3] == '=' ) return new LowLevelToken( NodeType.GreaterThanGreaterThanGreaterThanEquals, 4 );
                                }
                                return new LowLevelToken( NodeType.GreaterThanGreaterThanGreaterThan, 3 );
                            }
                            if( head[2] == '=' ) return new LowLevelToken( NodeType.GreaterThanGreaterThanEquals, 3 );
                        }
                        return new LowLevelToken( NodeType.GreaterThanGreaterThan, 2 );
                    }
                    if( c == '=' )
                    {
                        return new LowLevelToken( NodeType.GreaterThanEquals, 2 );
                    }
                    break;
            }
        }
        return new LowLevelToken( knownSingle, 1 );
    }

}
