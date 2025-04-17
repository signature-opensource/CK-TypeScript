using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Transform.Core;


/// <summary>
/// Hosts multiple <see cref="Language"/>.
/// This is NOT thread safe and should never be used concurrently.
/// </summary>
public sealed partial class TransformerHost
{
    readonly List<Language> _languages;
    readonly RootTransformLanguage _transformLanguage;
    bool _isLockedLanguages;

    /// <summary>
    /// Cached instance of a <see cref="TransformLanguage"/> for this host.
    /// </summary>
    public sealed class Language
    {
        readonly TransformLanguage _language;
        readonly TransformStatementAnalyzer _transformStatementAnalyzer;
        readonly IAnalyzer _targetAnalyzer;

        /// <summary>
        /// Gets the <see cref="TransformLanguage.LanguageName"/>.
        /// </summary>
        public string LanguageName => _language.LanguageName;

        /// <summary>
        /// Gets the transform language.
        /// </summary>
        public TransformLanguage TransformLanguage => _language;

        /// <summary>
        /// Gets the analyzer for the transform language.
        /// </summary>
        public TransformStatementAnalyzer TransformStatementAnalyzer => _transformStatementAnalyzer;

        /// <summary>
        /// Gets the target language annalyzer.
        /// </summary>
        public IAnalyzer TargetAnalyzer => _targetAnalyzer;

        internal Language( TransformerHost host, TransformLanguage language )
        {
            _language = language;
            (_transformStatementAnalyzer,_targetAnalyzer) = language.CreateAnalyzers( host );
        }
    }

    /// <summary>
    /// Initializes a host with at least the <see cref="RootTransformLanguage"/>.
    /// </summary>
    /// <param name="languages">Languages to register.</param>
    public TransformerHost( params IEnumerable<TransformLanguage> languages )
    {
        _transformLanguage = new RootTransformLanguage( this );
        _languages = new List<Language>();
        bool hasTransfomer = false;
        foreach( var language in languages )
        {
            EnsureLanguage( language );
            hasTransfomer |= language.IsTransformerLanguage;
        }
        if( !hasTransfomer )
        {
            _languages.Add( new Language( this, _transformLanguage ) );
        }
    }

    /// <summary>
    /// Gets the languages that this transfomer handles.
    /// </summary>
    public IReadOnlyList<Language> Languages => _languages;

    /// <summary>
    /// Gets whether no languages can be added or removed.
    /// </summary>
    public bool IsLockedLanguages => _isLockedLanguages;

    /// <summary>
    /// Locks the languages: <see cref="EnsureLanguage(TransformLanguage)"/> and <see cref="RemoveLanguage(TransformLanguage)"/>
    /// must not be called anymore.
    /// </summary>
    /// <returns>The locked list of supported languages.</returns>
    public IReadOnlyList<Language> LockLanguages()
    {
        _isLockedLanguages = true;
        return _languages;
    }

    /// <summary>
    /// Removes a language.
    /// <para>
    /// <see cref="IsLockedLanguages"/> must be false otherwise a <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="language">The language to remove.</param>
    /// <returns>True if the language has been removed, false if it was not found or if it is the transform language itself.</returns>
    public bool RemoveLanguage( TransformLanguage language )
    {
        Throw.CheckState( IsLockedLanguages is false );
        if( !language.IsTransformerLanguage )
        {
            var idx = _languages.FindIndex( l => l.LanguageName == language.LanguageName );
            if( idx >= 0 )
            {
                _languages.RemoveAt( idx );
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Adds a language if it is not already registered or finds its cached instance.
    /// <para>
    /// <see cref="IsLockedLanguages"/> must be false otherwise a <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="language">The language to add.</param>
    /// <returns>The cached language.</returns>
    public Language EnsureLanguage( TransformLanguage language )
    {
        Throw.CheckState( IsLockedLanguages is false );
        var l = _languages.FirstOrDefault( l => l.LanguageName == language.LanguageName );
        if( l == null )
        {
            l = new Language( this, language );
            _languages.Add( l );
        }
        return l;
    }

    /// <summary>
    /// Finds a registered <see cref="Language"/>.
    /// </summary>
    /// <param name="name">The language name.</param>
    /// <returns>The language or null if not found.</returns>
    public Language? Find( ReadOnlySpan<char> name ) => Find( _languages, name );

    /// <summary>
    /// Tries to find a <see cref="Language"/> from a file name or path.
    /// </summary>
    /// <param name="fileName">The file name or path.</param>
    /// <returns>The language or null.</returns>
    public Language? FindFromFilename( ReadOnlySpan<char> fileName )
    {
        foreach( var l in _languages )
        {
            if( l.TransformLanguage.IsLangageFilename( fileName ) )
            {
                return l;
            }
        }
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
    public TransformerFunction? TryParseFunction( IActivityMonitor monitor, ReadOnlyMemory<char> text ) => TryParseFunction( monitor, text, out var _ );

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
        var r = _transformLanguage.RootAnalyzer.TryParse( monitor, text );
        if( r == null )
        {
            hasError = true;
            return null;
        }
        hasError = false;
        var f = r.SourceCode.Spans.FirstOrDefault();
        Throw.DebugAssert( f == null || f is TransformerFunction );
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
        return _transformLanguage.RootAnalyzer.TryParseMultiple( monitor, text );
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
        var transformer = transformers.FirstOrDefault();
        Throw.CheckArgument( transformer is not null );

        Language? language = LocalFind( monitor, _languages, transformer, text );
        if( language == null ) return null;
        AnalyzerResult? r = language.TargetAnalyzer.TryParse( monitor, text.AsMemory() );
        if( r == null ) return null;

        var codeEditor = new SourceCodeEditor( language.TargetAnalyzer, r.SourceCode );
        if( !transformer.Apply( monitor, codeEditor ) ) return null;

        foreach( var t in transformers.Skip( 1 ) )
        {
            if( !t.Language.LanguageName.Equals( language.LanguageName, StringComparison.OrdinalIgnoreCase ) )
            {
                language = LocalFind( monitor, _languages, transformer, text );
                if( language == null || !codeEditor.Reparse( monitor, language.TargetAnalyzer ) )
                {
                    return null;
                }
            }
            if( !t.Apply( monitor, codeEditor ) )
            {
                return null;
            }
        }
        return codeEditor._code;

        static Language? LocalFind( IActivityMonitor monitor, List<Language> languages, TransformerFunction transformer, string text )
        {
            var l = Find( languages, transformer.Language.LanguageName );
            if( l == null )
            {
                monitor.Error( $"""
                            Unavailable language '{transformer.Language.LanguageName}'. Cannot apply:
                            {transformer}
                            On text:
                            {text}
                            """ );
                return null;
            }
            return l;
        }

    }

    static Language? Find( List<Language> languages, ReadOnlySpan<char> name )
    {
        foreach( var l in languages )
        {
            if( name.Equals( l.LanguageName, StringComparison.OrdinalIgnoreCase ) )
                return l;
        }
        return null;
    }

}
