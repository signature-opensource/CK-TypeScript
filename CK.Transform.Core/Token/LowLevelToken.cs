using System;

namespace CK.Transform.Core;

/// <summary>
/// Low-level token is a candidate token.
/// </summary>
/// <param name="TokenType">The detected candidate node type. Defaults to <see cref="TokenType.None"/>.</param>
/// <param name="Length">The candidate token length. Defaults to 0.</param>
public readonly record struct LowLevelToken( TokenType TokenType, int Length )
{
    /// <summary>
    /// Handles basic tokens from <see cref="BasicTokenType"/>.
    /// </summary>
    /// <param name="head">The head.</param>
    /// <returns>The low level token.</returns>
    public static LowLevelToken GetBasicTokenType( ReadOnlySpan<char> head )
    {
        if( head.Length == 0 ) return default;
        var c = head[0];
        var knownSingle = TokenTypeExtensions.GetSingleCharType( c );
        if( knownSingle == TokenType.None ) return default;
        if( head.Length > 1 )
        {
            c = head[1];
            switch( knownSingle )
            {
                case TokenType.Ampersand:
                    if( c == '&' )
                    {
                        if( head.Length > 2 && head[2] == '=' )
                        {
                            return new LowLevelToken( TokenType.AmpersandAmpersandEquals, 3 );
                        }
                        return new LowLevelToken( TokenType.AmpersandAmpersand, 2 );
                    }
                    if( c == '=' ) return new LowLevelToken( TokenType.AmpersandEquals, 2 );
                    break;
                case TokenType.Asterisk:
                    if( c == '=' ) return new LowLevelToken( TokenType.AsteriskEquals, 2 );
                    break;
                case TokenType.Bar:
                    if( c == '|' )
                    {
                        if( head.Length > 2 && head[2] == '=' )
                        {
                            return new LowLevelToken( TokenType.BarBarEquals, 3 );
                        }
                        return new LowLevelToken( TokenType.BarBar, 2 );
                    }

                    if( c == '=' ) return new LowLevelToken( TokenType.BarEquals, 2 );
                    break;
                case TokenType.Caret:
                    if( c == '=' ) return new LowLevelToken( TokenType.CaretEquals, 2 );
                    break;
                case TokenType.Colon:
                    if( c == ':' ) return new LowLevelToken( TokenType.ColonColon, 2 );
                    break;
                case TokenType.Dot:
                    if( c == '.' )
                    {
                        if( head.Length > 2 && head[2] == '.' )
                        {
                            return new LowLevelToken( TokenType.DotDotDot, 3 );
                        }
                        return new LowLevelToken( TokenType.DotDot, 2 );
                    }
                    break;
                case TokenType.Equals:
                    if( c == '=' )
                    {
                        if( head.Length > 2 && head[2] == '=' )
                        {
                            return new LowLevelToken( TokenType.EqualsEqualsEquals, 3 );
                        }
                        return new LowLevelToken( TokenType.EqualsEquals, 2 );
                    }
                    if( c == '>' )
                    {
                        return new LowLevelToken( TokenType.EqualsGreaterThan, 2 );
                    }
                    break;
                case TokenType.Exclamation:
                    if( c == '=' )
                    {
                        if( head.Length > 2 && head[2] == '=' )
                        {
                            return new LowLevelToken( TokenType.ExclamationEqualsEquals, 3 );
                        }
                        return new LowLevelToken( TokenType.ExclamationEquals, 2 );
                    }
                    break;
                case TokenType.Percent:
                    if( c == '=' ) return new LowLevelToken( TokenType.PercentEquals, 2 );
                    break;
                case TokenType.Minus:
                    if( c == '=' ) return new LowLevelToken( TokenType.MinusEquals, 2 );
                    if( c == '-' ) return new LowLevelToken( TokenType.MinusMinus, 2 );
                    break;
                case TokenType.Plus:
                    if( c == '=' ) return new LowLevelToken( TokenType.PlusEquals, 2 );
                    if( c == '+' ) return new LowLevelToken( TokenType.PlusPlus, 2 );
                    break;
                case TokenType.Slash:
                    if( c == '=' ) return new LowLevelToken( TokenType.SlashEquals, 2 );
                    break;
                case TokenType.LessThan:
                    if( c == '<' )
                    {
                        if( head.Length > 2 )
                        {
                            if( head[2] == '<' ) return new LowLevelToken( TokenType.LessThanLessThanLessThan, 3 );
                            if( head[2] == '=' ) return new LowLevelToken( TokenType.LessThanLessThanEquals, 3 );
                        }
                        return new LowLevelToken( TokenType.LessThanLessThan, 2 );
                    }
                    if( c == '=' )
                    {
                        return new LowLevelToken( TokenType.LessThanEquals, 2 );
                    }
                    break;
                case TokenType.GreaterThan:
                    if( c == '>' )
                    {
                        if( head.Length > 2 )
                        {
                            if( head[2] == '>' )
                            {
                                if( head.Length > 3 )
                                {
                                    if( head[3] == '=' ) return new LowLevelToken( TokenType.GreaterThanGreaterThanGreaterThanEquals, 4 );
                                }
                                return new LowLevelToken( TokenType.GreaterThanGreaterThanGreaterThan, 3 );
                            }
                            if( head[2] == '=' ) return new LowLevelToken( TokenType.GreaterThanGreaterThanEquals, 3 );
                        }
                        return new LowLevelToken( TokenType.GreaterThanGreaterThan, 2 );
                    }
                    if( c == '=' )
                    {
                        return new LowLevelToken( TokenType.GreaterThanEquals, 2 );
                    }
                    break;
            }
        }
        return new LowLevelToken( knownSingle, 1 );
    }

    /// <summary>
    /// Very simple sting parsing. The first character is the quote (typically ' or " but may be any other
    /// character) that is used as the closing quote. Backslach \ escapes the following characters whatever it is.
    /// </summary>
    /// <param name="head">The head to parse.</param>
    /// <returns>A low-level token with type <see cref="TokenType.GenericString"/> or <see cref="TokenType.ErrorUnterminatedString"/>.</returns>
    public static LowLevelToken BasicallyReadQuotedString( ReadOnlySpan<char> head )
    {
        var q = head[0];
        int iS = 0;
        bool escape = false;
        for(; ; )
        {
            if( ++iS == head.Length ) return new LowLevelToken( TokenType.ErrorUnterminatedString, iS );
            if( escape )
            {
                escape = false;
                continue;
            }
            var c = head[iS];
            if( c == q ) return new LowLevelToken( TokenType.GenericString, iS + 1 );
            escape = c == '\\';
        }
    }

}
