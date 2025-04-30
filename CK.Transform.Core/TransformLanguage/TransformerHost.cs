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
    readonly List<Language> _languages;
    readonly RootTransformLanguage _transformLanguage;
    bool _isLockedLanguages;

    /// <summary>
    /// Cached instance of a <see cref="TransformLanguage"/> for this host.
    /// </summary>
    public sealed class Language
    {
        readonly TransformerHost _host;
        readonly TransformLanguage _language;
        readonly TransformStatementAnalyzer _transformStatementAnalyzer;
        readonly ITargetAnalyzer _targetAnalyzer;
        readonly int _index;

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
        /// Gets the target language analyzer.
        /// </summary>
        public ITargetAnalyzer TargetAnalyzer => _targetAnalyzer;

        /// <summary>
        /// Gets the index in the <see cref="TransformerHost.Languages"/> list.
        /// </summary>
        public int Index => _index;

        /// <summary>
        /// Gets the host to which this language is bound.
        /// </summary>
        public TransformerHost Host => _host;

        internal Language( TransformerHost host, TransformLanguage language, int index )
        {
            _host = host;
            _language = language;
            _index = index;
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
            _languages.Add( new Language( this, _transformLanguage, _languages.Count ) );
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
    /// Locks the languages: <see cref="EnsureLanguage(TransformLanguage)"/> must not be called anymore.
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
    public Language? FindLanguage( ReadOnlySpan<char> name, bool withFileExtensions = true ) => FindLanguage( _languages, name, withFileExtensions );

    /// <summary>
    /// Tries to find a <see cref="Language"/> from a file name.
    /// </summary>
    /// <param name="fileName">The file name or path.</param>
    /// <param name="extension">The extension that matched.</param>
    /// <returns>The language or null.</returns>
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

        AnalyzerResult? r = transformer.Language.TargetAnalyzer.TryParse( monitor, text );
        if( r == null ) return null;

        var codeEditor = new SourceCodeEditor( transformer.Language, r.SourceCode );
        if( !transformer.Apply( monitor, codeEditor ) ) return null;

        foreach( var t in transformers.Skip( 1 ) )
        {
            if( t.Language != codeEditor.Language )
            {
                if( !codeEditor.Reparse( monitor, t.Language ) )
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
