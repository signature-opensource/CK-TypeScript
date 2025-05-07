using CK.Core;

namespace CK.Transform.Core;

/// <summary>
/// Extends <see cref="ITokenScanner"/>.
/// </summary>
public static class TokenScannerExtensions
{
    /// <summary>
    /// Provides a reusable and standard <see cref="Analyzer.DoParse(ref TokenizerHead)"/>
    /// implementation for analyzers that rely only on a <see cref="ITokenScanner"/>.
    /// <para>
    /// <see cref="ITokenScanner.GetNextToken(ref TokenizerHead)"/> is called until
    /// it returns a <see cref="TokenType.EndOfInput"/>.
    /// </para>
    /// </summary>
    /// <param name="scanner">This scanner.</param>
    /// <param name="head">The tokenizer head to analyze.</param>
    public static void StandardParse( this ITokenScanner scanner, ref TokenizerHead head )
    {
        while( scanner.GetNextToken( ref head ).TokenType != TokenType.EndOfInput ) ;
    }

    /// <summary>
    /// Calls <see cref="ITokenScanner.GetNextToken(ref TokenizerHead)"/> until a token
    /// of the provided <paramref name="type"/> is found or emits an error at the end of the input.
    /// </summary>
    /// <param name="scanner">This scanner.</param>
    /// <param name="head">The head.</param>
    /// <param name="type">The expected token type.</param>
    /// <returns>
    /// True when <see cref="TokenizerHead.LastToken"/> of the type has been found,
    /// false otherwise: all tokens have been analyzed, <see cref="TokenizerHead.EndOfInput"/> is available.
    /// </returns>
    public static bool SkipTo( this ITokenScanner scanner, ref TokenizerHead head, TokenType type )
    {
        for(; ; )
        {
            var t = scanner.GetNextToken( ref head );
            if( t.TokenType is TokenType.EndOfInput )
            {
                head.AppendError( $"Missing '{type}' token.", 0 );
                return false;
            }
            if( t.TokenType == type )
            {
                return true;
            }
        }
    }

}
