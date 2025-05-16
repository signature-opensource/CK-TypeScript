using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Transform.Core;

/// <summary>
/// Hosts multiple <see cref="Language"/>.
/// This is NOT thread safe and should never be used concurrently.
/// <para>
/// The nested <see cref="Language"/> class binds a <see cref="TransformLanguage"/> to
/// a host instance. New languages can be added until <see cref="LockLanguages"/> is called
/// and cannot be removed.
/// </para>
/// </summary>
public sealed partial class TransformerHost
{
    readonly Language _rootLanguage;
    readonly List<Language> _languages;
    readonly List<Language> _autoTLanguages;
    bool _isLockedLanguages;

    /// <summary>
    /// Initializes a host with an initial set of supported languages.
    /// </summary>
    /// <param name="languages">Languages to register.</param>
    public TransformerHost( params IEnumerable<TransformLanguage> languages )
    {
        _languages = new List<Language>();
        _autoTLanguages = new List<Language>();
        foreach( var language in languages )
        {
            if( !language.IsAutoLanguage )
            {
                RegisterLanguage( language );
            }
        }
        _rootLanguage = new Language( this, RootTransformLanguage, _languages.Count );
        _languages.Add( _rootLanguage );
    }

    /// <summary>
    /// Gets the registered languages that this transfomer handles.
    /// </summary>
    public IReadOnlyList<Language> Languages => _languages;

    /// <summary>
    /// Gets whether no languages can be added or removed.
    /// </summary>
    public bool IsLockedLanguages => _isLockedLanguages;

    /// <summary>
    /// Locks the languages: <see cref="RegisterLanguage(TransformLanguage)"/> must not be called anymore.
    /// </summary>
    /// <returns>The locked list of supported languages.</returns>
    public IReadOnlyList<Language> LockLanguages()
    {
        _isLockedLanguages = true;
        return _languages;
    }

    /// <summary>
    /// Adds a language if it is not already registered or finds its cached instance.
    /// <para>
    /// <see cref="IsLockedLanguages"/> must be false otherwise a <see cref="InvalidOperationException"/> is thrown
    /// and <see cref="TransformLanguage.IsAutoLanguage"/> must be false otherwise an <see cref="ArgumentException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="language">The language to add.</param>
    /// <returns>The cached language.</returns>
    public Language RegisterLanguage( TransformLanguage language )
    {
        Throw.CheckState( IsLockedLanguages is false );
        Throw.CheckArgument( language.IsAutoLanguage is false );
        var l = FindLanguage( _languages, language.LanguageName, withFileExtensions: false );
        if( l == null )
        {
            l = new Language( this, language, _languages.Count );
            _languages.Add( l );
        }
        return l;
    }

    /// <summary>
    /// Finds a registered <see cref="Language"/>.
    /// </summary>
    /// <param name="name">The language name. A leading '.' is silently handled.</param>
    /// <param name="withFileExtensions">
    /// False to use only <see cref="TransformLanguage.LanguageName"/> and ignore <see cref="TransformLanguage.FileExtensions"/>.
    /// </param>
    /// <returns>The language or null if not found.</returns>
    public Language? FindRegisteredLanguage( ReadOnlySpan<char> name, bool withFileExtensions = true ) => FindLanguage( _languages, name, withFileExtensions );

    /// <summary>
    /// Finds a <see cref="Language"/> either a registered one or a <see cref="TransformLanguage.IsAutoLanguage"/> one.
    /// </summary>
    /// <param name="name">The language name. A leading '.' is silently handled.</param>
    /// <param name="withFileExtensions">
    /// False to use only <see cref="TransformLanguage.LanguageName"/> and ignore <see cref="TransformLanguage.FileExtensions"/>.
    /// </param>
    /// <returns>The language or null if not found.</returns>
    public Language? FindLanguage( ReadOnlySpan<char> name, bool withFileExtensions )
    {
        if( name.Length > 0 )
        {
            if( name[0] == '.' ) name = name.Slice( 1 );
            if( name.Length > 0 )
            {
                // First find the name as-is.
                // It can be a final one <sql>: we found its (<sql.t>,<sql>) language.
                // It may be a transfomer language <sql.t>:
                // - If a (<sql.t.t>,<sql.t>) language has been registered, then there is
                //   a specific implementation of the <sql.t.t> transform analyzer.
                // - If no specific implementation exists, we create and register an auto language
                //   (<sql.t.t>,<sql.t>) where the TransformAnalyzer will use the <sql.t> as its
                //   target language analyzer (tokens will be specifically handled).
                // - But we do this only for one level of transformer: above the generic transformer
                //   language is used (that is only able to handle the generic transfomr language).
                Language? language = FindLanguage( _languages, name, withFileExtensions )
                                     ?? FindLanguage( _autoTLanguages, name, withFileExtensions );
                if( language != null ) return language;

                int transformerLevel = 0;
                var subName = name;
                while( RemoveTransformerExtension( ref subName ) )
                {
                    // Don't lookup in the _autoTLanguages here: we don't (currently) want to
                    // create useless instances of AutoTransformLanguage with a target
                    // analyzer that is a standard LangageTransformAnalyzer.
                    //
                    // If the final target transformed language needs one day to be accessible
                    // (a "PeeledTargetLanguageAnalyzer"), then we'll need to build the full linked
                    // list. And the RootTransformLanguage may no more exist.
                    //
                    language = FindLanguage( _languages, subName, withFileExtensions );
                    if( language != null )
                    {
                        break;
                    }
                    ++transformerLevel;
                }
                if( language == null || transformerLevel == 0 )
                {
                    // No luck: this is not a ".t" name or we cannot find a target language.
                    return null;
                }
                // This is a transformer of a known language.
                if( transformerLevel == 1 )
                {
                    // We are on a "X.t" where "X" is known.
                    var auto = new AutoTransformLanguage( language );
                    language = new Language( this, auto );
                    _autoTLanguages.Add( language );
                    return language;
                }
                // Uses the root transform language.
                return _rootLanguage;
            }
        }
        return null;

        static bool RemoveTransformerExtension( ref ReadOnlySpan<char> name )
        {
            if( name.EndsWith( ".t" ) || name.EndsWith( ".T" ) )
            {
                name = name[..^2];
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Tries to find a registered <see cref="Language"/> from a file name.
    /// </summary>
    /// <param name="fileName">The file name or path.</param>
    /// <param name="extension">The extension that matched.</param>
    /// <returns>The language or null.</returns>
    [Obsolete( "Shoud not be used... or used differently." )]
    public Language? FindFromFilename( ReadOnlySpan<char> fileName, out ReadOnlySpan<char> extension )
    {
        foreach( var l in _languages )
        {
            extension = l.TransformLanguage.CheckLangageFilename( fileName );
            if( extension.Length > 0 )
            {
                return l;
            }
        }
        extension = default;
        return null;
    }

    /// <inheritdoc cref="TryParseFunction(IActivityMonitor,ReadOnlyMemory{char})"/>
    public TransformerFunction? TryParseFunction( IActivityMonitor monitor, string text ) => TryParseFunction( monitor, text.AsMemory() );

    /// <inheritdoc cref="TryParseFunction(IActivityMonitor, ReadOnlyMemory{char}, out bool)"/>
    public TransformerFunction? TryParseFunction( IActivityMonitor monitor, string text, out bool hasError ) => TryParseFunction( monitor, text.AsMemory(), out hasError );

    /// <summary>
    /// Tries to parse a <see cref="TransformerFunction"/>. Returns null on error (errors are logged) or if the text doesn't
    /// start with a <c>create</c> token.
    /// </summary>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="text">the text to parse.</param>
    /// <returns>The function or null on error.</returns>
    public TransformerFunction? TryParseFunction( IActivityMonitor monitor, ReadOnlyMemory<char> text ) => TryParseFunction( monitor, text, out _ );

    /// <summary>
    /// Tries to parse a <see cref="TransformerFunction"/>.
    /// Returns null on error (errors are logged and <paramref name="hasError"/> is true) or if
    /// the text doesn't start with a <c>create</c> token.
    /// </summary>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="text">the text to parse.</param>
    /// <param name="hasError">True if an error occurred.</param>
    /// <returns>The function or null on error.</returns>
    public TransformerFunction? TryParseFunction( IActivityMonitor monitor, ReadOnlyMemory<char> text, out bool hasError )
    {
        var r = _rootLanguage.TransformLanguageAnalyzer.TryParse( monitor, text );
        if( r == null )
        {
            hasError = true;
            return null;
        }
        hasError = false;
        var f = r.SourceCode.Spans.FirstOrDefault();
        Throw.DebugAssert( f == null || f is TransformerFunction && f.CheckValid() );
        return Unsafe.As<TransformerFunction?>( f );
    }

    /// <inheritdoc cref="TryParseFunctions(IActivityMonitor, ReadOnlyMemory{char})"/>
    public List<TransformerFunction>? TryParseFunctions( IActivityMonitor monitor, string text ) => TryParseFunctions( monitor, text.AsMemory() );

    /// <summary>
    /// Tries to parse multiple <see cref="TransformerFunction"/>. Returns null on error.
    /// </summary>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="text">the text to parse.</param>
    /// <returns>The functions or null on error.</returns>
    public List<TransformerFunction>? TryParseFunctions( IActivityMonitor monitor, ReadOnlyMemory<char> text )
    {
        return _rootLanguage.TransformLanguageAnalyzer.TryParseMultiple( monitor, text );
    }

    /// <summary>
    /// Applies a sequence of transformers to an initial <paramref name="text"/>.
    /// </summary>
    /// <param name="monitor">The monitor that will receive logs and errors.</param>
    /// <param name="text">The text to transform.</param>
    /// <param name="transformers">The transformers to apply in order.</param>
    /// <returns>The transformed result on success and null if an error occurred.</returns>
    public SourceCode? Transform( IActivityMonitor monitor,
                                  string text,
                                  params IEnumerable<TransformerFunction> transformers )
    {
        return Transform( monitor, text.AsMemory(), transformers );
    }

    /// <inheritdoc cref="Transform(IActivityMonitor, string, IEnumerable{TransformerFunction})"/>
    public SourceCode? Transform( IActivityMonitor monitor,
                                  ReadOnlyMemory<char> text,
                                  params IEnumerable<TransformerFunction> transformers )
    {
        Throw.CheckArgument( transformers.All( t => t.Language.Host == this ) );
        var transformer = transformers.FirstOrDefault();
        Throw.CheckArgument( transformer is not null );

        AnalyzerResult? r = transformer.Language.TargetLanguageAnalyzer.TryParse( monitor, text );
        if( r == null ) return null;

        var codeEditor = new SourceCodeEditor( monitor, r.SourceCode, transformer.Language );
        try
        {
            transformer.Apply( codeEditor );
            if( codeEditor.HasError ) return null;

            foreach( var t in transformers.Skip( 1 ) )
            {
                if( t.Language != codeEditor.Language )
                {
                    if( !codeEditor.Reparse( t.Language ) )
                    {
                        return null;
                    }
                }
                t.Apply( codeEditor );
                if( codeEditor.HasError ) return null;
            }
            return codeEditor.Code;
        }
        finally
        {
            codeEditor.Dispose();
        }
    }

    static Language? FindLanguage( List<Language> languages, ReadOnlySpan<char> name, bool withFileExtensions )
    {
        if( name.Length > 0 )
        {
            if( name[0] == '.' ) name = name.Slice( 1 );
            if( name.Length > 0 )
            {
                if( withFileExtensions )
                {
                    foreach( var l in languages )
                    {
                        foreach( var f in l.TransformLanguage.FileExtensions )
                        {
                            if( name.Equals( f.AsSpan( 1 ), StringComparison.OrdinalIgnoreCase ) )
                                return l;
                        }
                    }
                }
                else
                {
                    foreach( var l in languages )
                    {
                        if( name.Equals( l.LanguageName, StringComparison.OrdinalIgnoreCase ) )
                            return l;
                    }
                }
            }
        }
        return null;
    }
}
