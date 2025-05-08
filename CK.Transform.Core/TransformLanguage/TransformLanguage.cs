using CK.Core;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Abstract factory for a <see cref="TargetLanguageAnalyzer"/> and its associated
/// transfom <see cref="LanguageTransformAnalyzer"/>.
/// <para>
/// A language is identified by its <see cref="LanguageName"/> and its <see cref="FileExtensions"/>
/// in a <see cref="TransformerHost"/>.
/// </para>
/// <para>
/// Implementations should expose a public default constructor to support <see cref="TransformerHost"/>
/// marshalling.
/// </para>
/// </summary>
public abstract class TransformLanguage
{
    readonly string _languageName;
    readonly ImmutableArray<string> _fileExtensions;

    /// <summary>
    /// Initializes a language with its name.
    /// </summary>
    /// <param name="languageName">
    /// The language name in PascalCase.
    /// Must not have a leading '.' and must appear in the <paramref name="fileExtensions"/> (file
    /// extensions use <see cref="StringComparison.OrdinalIgnoreCase"/>).
    /// </param>
    /// <param name="fileExtensions">
    /// File extensions must not be empty and all must start with a dot (like ".cs").
    /// The ".<paramref name="languageName"/>" must appear explicitly.
    /// </param>
    protected TransformLanguage( string languageName, params ImmutableArray<string> fileExtensions )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( languageName );
        Throw.CheckArgument( languageName.Equals( TransformerHost.RootTransformLanguage.LanguageName, StringComparison.OrdinalIgnoreCase ) is false );
        Throw.CheckArgument( languageName[0] != '.' );
        Throw.CheckArgument( fileExtensions.Length > 0
                             && fileExtensions.All( e => e.Length >= 2 && e[0] == '.' )
                             && fileExtensions.Any( e => languageName.AsSpan().Equals( e.AsSpan(1), StringComparison.OrdinalIgnoreCase ) ) );
        _languageName = languageName;
        _fileExtensions = fileExtensions;
    }

    private protected TransformLanguage()
    {
        _languageName = TransformerHost._transformLanguageName;
        _fileExtensions = [".transform", ".t"];
    }

    /// <summary>
    /// Gets the language name.
    /// </summary>
    public string LanguageName => _languageName;

    /// <summary>
    /// Gets one or more file extensions for this language. They start with a dot (like ".cs"),
    /// contains the ".<see cref="LanguageName"/>" (typically in ower case).
    /// <see cref="StringComparison.OrdinalIgnoreCase"/> is used by <see cref="CheckLangageFilename(ReadOnlySpan{char})"/>.
    /// </summary>
    public ImmutableArray<string> FileExtensions => _fileExtensions;

    /// <summary>
    /// Tests the file name against the <see cref="FileExtensions"/>.
    /// </summary>
    /// <param name="fileName">The file name. May be a path.</param>
    /// <returns>The extension that matched on success, an empty span otherwise.</returns>
    public ReadOnlySpan<char> CheckLangageFilename( ReadOnlySpan<char> fileName )
    {
        foreach( var e in _fileExtensions )
        {
            if( fileName.EndsWith( e, StringComparison.OrdinalIgnoreCase ) )
            {
                return e;
            }
        }
        return default;
    }

    /// <summary>
    /// Gets whether this is the "Transform" language.
    /// </summary>
    public bool IsTransformerLanguage => ReferenceEquals( _languageName, TransformerHost._transformLanguageName );

    /// <summary>
    /// Must create the transform statement analyzer (and its <see cref="LanguageTransformAnalyzer.TargetAnalyzer"/>).
    /// </summary>
    /// <param name="language">The language for which the analyzers are created.</param>
    /// <returns>The transform langage analyzer.</returns>
    internal protected abstract LanguageTransformAnalyzer CreateAnalyzer( TransformerHost.Language language );

    /// <summary>
    /// Overridden to return the <see cref="LanguageName"/>.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => _languageName;

}
