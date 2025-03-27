using CK.Core;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Abstract factory for a target language <see cref="IAnalyzer"/> and its associated
/// transfom <see cref="TransformStatementAnalyzer"/>.
/// <para>
/// A language is identified by its <see cref="LanguageName"/> in a <see cref="TransformerHost"/>
/// and its <see cref="FileExtensions"/> must also be unique.
/// </para>
/// </summary>
public abstract class TransformLanguage
{
    readonly string _languageName;
    readonly ImmutableArray<string> _fileExtensions;

    /// <summary>
    /// Initializes a language with its name.
    /// </summary>
    /// <param name="languageName">The language name.</param>
    /// <param name="fileExtensions">File extensions must not be empty andd all must start with a dot (like ".cs").</param>
    protected TransformLanguage( string languageName, params ImmutableArray<string> fileExtensions )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( languageName );
        Throw.CheckArgument( fileExtensions.Length > 0 && fileExtensions.All( e => e.Length >= 2 && e[0] == '.' ) );
        _languageName = languageName;
        _fileExtensions = fileExtensions;
    }

    /// <summary>
    /// Gets the language name.
    /// </summary>
    public string LanguageName => _languageName;

    /// <summary>
    /// Gets one or more file extensions for this language. They start with a dot (like ".cs").
    /// </summary>
    public ImmutableArray<string> FileExtensions => _fileExtensions;

    /// <summary>
    /// Tests the file name against the <see cref="FileExtensions"/>.
    /// </summary>
    /// <param name="fileName">The file name. May be a path.</param>
    /// <returns>True if the file can be handled by this language analyzer.</returns>
    public bool IsLangageFilename( ReadOnlySpan<char> fileName )
    {
        foreach( var e in _fileExtensions )
        {
            if( fileName.EndsWith( e, StringComparison.Ordinal ) ) return true;
        }
        return false;
    }

    /// <summary>
    /// Gets whether this is the Transform language.
    /// </summary>
    public bool IsTransformerLanguage => ReferenceEquals( _languageName, TransformerHost._transformLanguageName );

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
