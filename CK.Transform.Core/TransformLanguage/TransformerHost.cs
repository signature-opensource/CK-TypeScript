using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Transform.Core;


/// <summary>
/// Hosts multiple <see cref="TransformLanguageOld"/>.
/// This is NOT thread safe and should never be used concurrently.
/// </summary>
public sealed partial class TransformerHost
{
    readonly List<Language> _languages;
    readonly RootTransformLanguage _transformLanguage;

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
    public TransformerHost( params TransformLanguage[] languages )
    {
        _transformLanguage = new RootTransformLanguage( this );
        _languages = new List<Language>();
        foreach( var language in languages ) EnsureLanguage( language );
        if( Find( _languages, RootTransformLanguage._languageName ) == null )
        {
            _languages.Add( new Language( this, _transformLanguage ) );
        }
    }

    /// <summary>
    /// Gets the languages that this transfomer handles.
    /// </summary>
    public IReadOnlyList<Language> Languages => _languages;

    /// <summary>
    /// Removes a language.
    /// </summary>
    /// <param name="language">The language to remove.</param>
    /// <returns>Tre if the language has been removed, false if it was not found.</returns>
    public bool RemoveLanguage( TransformLanguage language )
    {
        var idx = _languages.FindIndex( l => l.TransformLanguage != _transformLanguage && l.LanguageName == language.LanguageName );
        if( idx >= 0 )
        {
            _languages.RemoveAt( idx );
            return true;
        }
        return false;
    }

    /// <summary>
    /// Adds a language if it is not already registered.
    /// </summary>
    /// <param name="language">The language to add.</param>
    public void EnsureLanguage( TransformLanguage language )
    {
        var l = _languages.FirstOrDefault( l => l.LanguageName == language.LanguageName );
        if( l == null )
        {
            _languages.Add( new Language( this, language ) );
        }
    }

    /// <summary>
    /// Finds a registered <see cref="Language"/>.
    /// </summary>
    /// <param name="name">The language name.</param>
    /// <returns>The language or null if not found.</returns>
    public Language? Find( ReadOnlySpan<char> name ) => Find( _languages, name );

    /// <inheritdoc cref="TryParseFunction(IActivityMonitor,ReadOnlyMemory{char})"/>
    public TransformerFunction? TryParseFunction( IActivityMonitor monitor, string text ) => TryParseFunction( monitor, text.AsMemory() );

    /// <summary>
    /// Parses a <see cref="TransformerFunction"/> or throws if it cannot be parsed.
    /// </summary>
    /// <param name="text">the text to parse.</param>
    /// <returns>The function.</returns>
    public TransformerFunction? TryParseFunction( IActivityMonitor monitor, ReadOnlyMemory<char> text )
    {
        var r =_transformLanguage.RootAnalyzer.TryParse( monitor, text );
        if( r == null ) return null;
        var f = r.SourceCode.Spans.FirstOrDefault();
        Throw.DebugAssert( f is TransformerFunction );
        return Unsafe.As<TransformerFunction>( f );
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
