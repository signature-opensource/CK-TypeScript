using CK.Core;
using CK.Transform.Core;
using System;

namespace CK.Transform.Core;

/// <summary>
/// Abstract factory for a target language <see cref="IAnalyzer"/> and its associated
/// transfom <see cref="TransformStatementAnalyzer"/>.
/// </summary>
public abstract class TransformLanguage
{
    readonly string _languageName;

    /// <summary>
    /// Initializes a language with its name.
    /// </summary>
    /// <param name="languageName">The language name.</param>
    protected TransformLanguage( string languageName )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( languageName );
        _languageName = languageName;
    }

    /// <summary>
    /// Gets the language name.
    /// </summary>
    public string LanguageName => _languageName;

    /// <summary>
    /// Must create the transforme statement analyzer and the target language analyzer.
    /// <para>
    /// The <see cref="TransformStatementAnalyzer"/> can reference the <see cref="IAnalyzer"/>
    /// to delegate some pattern matches to the target language analyzer.
    /// </para>
    /// </summary>
    /// <param name="host">The transformer host.</param>
    /// <returns>The transform statement and target language analyzer.</returns>
    internal protected abstract (TransformStatementAnalyzer,IAnalyzer) CreateAnalyzers( TransformerHost host );

    /// <summary>
    /// Optional extension point that can handle the "on ...." specifier.
    /// Returns null at this level.
    /// </summary>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="target">A <see cref="Token"/> that describes the target.</param>
    /// <returns>A <see cref="NodeScopeBuilder"/>or null.</returns>
    internal protected virtual NodeScopeBuilder? HandleTransformerTarget( IActivityMonitor monitor, Token target ) => null;

    /// <summary>
    /// Supports the minimal token set required by any transform language:
    /// <list type="bullet">
    ///     <item><see cref="TokenType.GenericIdentifier"/> that at least handles "Ascii letter[Ascii letter or digit]*".</item>
    ///     <item><see cref="TokenType.DoubleQuote"/>.</item>
    ///     <item><see cref="TokenType.LessThan"/>.</item>
    ///     <item><see cref="TokenType.Dot"/>.</item>
    ///     <item><see cref="TokenType.SemiColon"/>.</item>
    /// </list>
    /// <para>
    /// A <see cref="TransformStatementAnalyzer"/> can implement <see cref="ILowLevelTokenizer"/> to support other low level token type.
    /// Such implementations should first handle specific tokens or extended token definition (such as a more complex <see cref="TokenType.GenericIdentifier"/>)
    /// and fallbacks to call this public helper.
    /// </para>
    /// </summary>
    /// <param name="head">The head.</param>
    /// <returns>The low level token.</returns>
    public static LowLevelToken MinimalTransformerLowLevelTokenize( ReadOnlySpan<char> head )
    {
        var c = head[0];
        if( char.IsAsciiLetter( c ) )
        {
            int iS = 0;
            while( ++iS < head.Length && char.IsAsciiLetterOrDigit( head[iS] ) ) ;
            return new LowLevelToken( TokenType.GenericIdentifier, iS );
        }
        if( c == '"' )
        {
            return new LowLevelToken( TokenType.DoubleQuote, 1 );
        }
        if( c == '.' )
        {
            return new LowLevelToken( TokenType.Dot, 1 );
        }
        if( c == ';' )
        {
            return new LowLevelToken( TokenType.SemiColon, 1 );
        }
        if( c == '<' )
        {
            return new LowLevelToken( TokenType.LessThan, 1 );
        }
        return default;
    }

    /// <summary>
    /// Overridden to return the <see cref="LanguageName"/>.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => _languageName;

}
